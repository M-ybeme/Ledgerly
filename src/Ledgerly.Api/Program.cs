using System.Text;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;
using Ledgerly.Api.Auth;
using Ledgerly.Infrastructure.Auth;
using Ledgerly.Application.Accounts;
using Ledgerly.Application.Auth;
using Ledgerly.Application.Budget;
using Ledgerly.Application.Credit;
using Ledgerly.Application.Dashboard;
using Ledgerly.Application.Debts;
using Ledgerly.Application.Income;
using Ledgerly.Application.Scenarios;
using Ledgerly.Infrastructure.Services;
using Ledgerly.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/ledgerly-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Rate limiting — auth endpoints only (5 requests / 60 seconds per IP)
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("auth", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromSeconds(60);
        o.SegmentsPerWindow = 6;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("LedgerlyDb")!);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Application services
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<DebtAccountService>();
builder.Services.AddScoped<ScenarioService>();
builder.Services.AddScoped<DebtProjectionService>();
builder.Services.AddScoped<ScenarioComparisonService>();
builder.Services.AddScoped<ActualPaymentService>();
builder.Services.AddScoped<DriftService>();
builder.Services.AddScoped<BudgetCategoryService>();
builder.Services.AddScoped<BudgetPlanService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<BudgetSummaryService>();
builder.Services.AddScoped<CreditProfileService>();
builder.Services.AddScoped<CreditScoreService>();
builder.Services.AddScoped<IncomeSourceService>();
builder.Services.AddScoped<PlannedExpenseService>();
builder.Services.AddScoped<MonthlyBudgetService>();
builder.Services.AddScoped<FinancialSummaryService>();
builder.Services.AddScoped<SavingsGoalService>();
builder.Services.AddHostedService<OverdueExpenseNotifier>();

// Auth services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<JwtTokenService>();

// JWT authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is required.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Ledgerly";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LedgerlyUsers";

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    })
    .AddCookie("ExternalCookie", options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
        options.CallbackPath = "/signin-google"; // middleware handles OAuth response here
        options.SignInScheme = "ExternalCookie";
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.IsEssential = true;
    });

builder.Services.AddAuthorization();

// CORS
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5001")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins)
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()));

// Infrastructure services (includes Identity)
builder.Services.AddLedgerlyInfrastructure(builder.Configuration);

// AddIdentity (above) overwrites DefaultChallengeScheme to cookie auth via a Configure action.
// PostConfigure runs after ALL Configure actions, so this is the final word.
builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
});

var app = builder.Build();

// Auto-migrate and seed on every startup (safe — EF skips already-applied migrations)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Ledgerly.Infrastructure.Data.LedgerlyDbContext>();
    await db.Database.MigrateAsync();
}
await DbSeeder.SeedDemoUserAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

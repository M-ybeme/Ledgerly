using System.Text;
using Ledgerly.Api.Auth;
using Ledgerly.Infrastructure.Auth;
using Ledgerly.Application.Accounts;
using Ledgerly.Application.Auth;
using Ledgerly.Application.Budget;
using Ledgerly.Application.Credit;
using Ledgerly.Application.Debts;
using Ledgerly.Application.Income;
using Ledgerly.Application.Scenarios;
using Ledgerly.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

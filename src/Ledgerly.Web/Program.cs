using Ledgerly.Web.Auth;
using Ledgerly.Web.Components;
using Ledgerly.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

// Auth
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.LoginPath = "/login");
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthTokenService>();
builder.Services.AddScoped<AuthenticationStateProvider, LedgerlyAuthStateProvider>();
builder.Services.AddTransient<BearerTokenHandler>();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5122";

// Public API client (no bearer token — auth endpoints)
builder.Services.AddHttpClient<AuthApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl));

// Protected API clients (bearer token injected)
builder.Services.AddHttpClient<AccountsApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<DebtAccountsApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<ScenariosApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<BudgetCategoriesApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<BudgetPlansApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<TransactionsApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<CreditApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

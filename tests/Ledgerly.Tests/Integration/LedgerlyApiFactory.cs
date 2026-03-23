using Ledgerly.Application.Auth;
using Ledgerly.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ledgerly.Tests.Integration;

/// <summary>
/// Spins up the real ASP.NET Core pipeline against an EF InMemory database
/// with SendGrid replaced by a no-op stub.  Each test class that implements
/// IClassFixture&lt;LedgerlyApiFactory&gt; shares one instance of the factory;
/// tests that need a clean database should call CreateClient() which uses a
/// shared InMemory store, or create a dedicated factory instance.
/// </summary>
public sealed class LedgerlyApiFactory : WebApplicationFactory<Program>
{
    // Unique DB name per factory instance so parallel test classes don't share state
    private readonly string _dbName = Guid.NewGuid().ToString();

    public LedgerlyApiFactory()
    {
        // With the minimal-API hosting model, WebApplication.CreateBuilder() reads
        // configuration eagerly — BEFORE WebApplicationFactory's ConfigureAppConfiguration
        // callbacks run.  Setting real environment variables here ensures the values are
        // available when Program.cs reads builder.Configuration["Jwt:Secret"] etc.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT",  "Testing");
        Environment.SetEnvironmentVariable("Jwt__Secret",             "ledgerly-test-secret-key-32chars!!");
        Environment.SetEnvironmentVariable("Jwt__Issuer",             "LedgerlyTest");
        Environment.SetEnvironmentVariable("Jwt__Audience",           "LedgerlyTestUsers");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes",      "60");
        // Dummy connection string — replaced by InMemory DbContext below
        Environment.SetEnvironmentVariable("ConnectionStrings__LedgerlyDb", "Host=test;Database=test");
        Environment.SetEnvironmentVariable("SendGrid__ApiKey",        "test");
        Environment.SetEnvironmentVariable("SendGrid__FromEmail",     "test@test.com");
        Environment.SetEnvironmentVariable("Web__BaseUrl",            "http://localhost");
        // Google OAuth — provide empty values to satisfy options validation
        Environment.SetEnvironmentVariable("Google__ClientId",        "test-client-id");
        Environment.SetEnvironmentVariable("Google__ClientSecret",    "test-client-secret");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real Npgsql DbContext registration
            services.RemoveAll<DbContextOptions<LedgerlyDbContext>>();
            services.RemoveAll<LedgerlyDbContext>();

            // Register InMemory DbContext (ICurrentUserService is still resolved from DI)
            services.AddDbContext<LedgerlyDbContext>((sp, options) =>
                options.UseInMemoryDatabase(_dbName));

            // No-op email service — prevents real SendGrid calls during tests
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService, NoOpEmailService>();
        });
    }
}

file sealed class NoOpEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody) => Task.CompletedTask;
}

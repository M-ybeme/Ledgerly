using Ledgerly.Application.Accounts;
using Ledgerly.Application.Auth;
using Ledgerly.Application.Budget;
using Ledgerly.Application.Credit;
using Ledgerly.Application.Debts;
using Ledgerly.Application.Scenarios;
using Ledgerly.Infrastructure.Accounts;
using Ledgerly.Infrastructure.Auth;
using Ledgerly.Infrastructure.Budget;
using Ledgerly.Infrastructure.Credit;
using Ledgerly.Infrastructure.Data;
using Ledgerly.Infrastructure.Debts;
using Ledgerly.Infrastructure.Scenarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ledgerly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLedgerlyInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<LedgerlyDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("LedgerlyDb")));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<LedgerlyDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IEmailService, SendGridEmailService>();

        services.AddScoped<IAccountRepository, EfAccountRepository>();
        services.AddScoped<IDebtAccountRepository, EfDebtAccountRepository>();
        services.AddScoped<IScenarioRepository, EfScenarioRepository>();
        services.AddScoped<IActualPaymentRepository, EfActualPaymentRepository>();
        services.AddScoped<ICreditProfileRepository, EfCreditProfileRepository>();
        services.AddScoped<IBudgetCategoryRepository, EfBudgetCategoryRepository>();
        services.AddScoped<IBudgetPlanRepository, EfBudgetPlanRepository>();
        services.AddScoped<ITransactionRepository, EfTransactionRepository>();

        return services;
    }
}

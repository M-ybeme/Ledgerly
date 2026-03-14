using Ledgerly.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ledgerly.Infrastructure.Auth;

public static class DbSeeder
{
    private const string DemoEmail = "demo@ledgerly.dev";
    private const string DemoPassword = "Demo1234!";

    public static async Task SeedDemoUserAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(DemoEmail) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Demo user seed failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }
}

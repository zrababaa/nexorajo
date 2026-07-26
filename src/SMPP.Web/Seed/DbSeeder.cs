using Microsoft.AspNetCore.Identity;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;

namespace SMPP.Web.Seed;

/// <summary>
/// Ensures the 2 roles exist, a Superadmin account exists, and a test Account exists under it,
/// so the app is usable on a fresh database without a manual SQL insert.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var superadminUsername = configuration["Seed:SuperadminUsername"] ?? "admin";
        var superadminEmail = configuration["Seed:SuperadminEmail"] ?? "admin@nexora.local";
        var superadminPassword = configuration["Seed:SuperadminPassword"] ?? "ChangeMe123!";

        var superadmin = await userManager.FindByNameAsync(superadminUsername);
        if (superadmin is null)
        {
            superadmin = new ApplicationUser
            {
                UserName = superadminUsername,
                Email = superadminEmail,
                EmailConfirmed = true,
                FullName = "Superadmin",
                Role = UserRole.Superadmin,
                IsActive = true,
                Balance = 0,
            };

            var result = await userManager.CreateAsync(superadmin, superadminPassword);
            if (!result.Succeeded)
            {
                return;
            }
            await userManager.AddToRoleAsync(superadmin, RoleNames.Superadmin);
        }

        var testUsername = configuration["Seed:TestAccountUsername"] ?? "testaccount";
        if (await userManager.FindByNameAsync(testUsername) is null)
        {
            var testAccount = new ApplicationUser
            {
                UserName = testUsername,
                Email = configuration["Seed:TestAccountEmail"] ?? "testaccount@nexora.local",
                EmailConfirmed = true,
                FullName = "Test Account",
                Role = UserRole.Account,
                IsActive = true,
                Balance = 1000,
                RatePerMessage = 0.05m,
                SenderId = "NEXORA",
                CreatedByUserId = superadmin.Id,
            };

            var result = await userManager.CreateAsync(testAccount, configuration["Seed:TestAccountPassword"] ?? "ChangeMe123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(testAccount, RoleNames.Account);
            }
        }
    }
}

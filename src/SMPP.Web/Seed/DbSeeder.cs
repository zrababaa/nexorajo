using Microsoft.AspNetCore.Identity;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;

namespace SMPP.Web.Seed;

/// <summary>
/// Ensures the 3 roles exist and that at least one Superadmin account exists, so the app is
/// usable on a fresh database without a manual SQL insert.
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

        var superadminEmail = configuration["Seed:SuperadminEmail"] ?? "admin@smpp.local";
        var superadminPassword = configuration["Seed:SuperadminPassword"] ?? "ChangeMe123!";

        if (await userManager.FindByEmailAsync(superadminEmail) is not null)
        {
            return;
        }

        var superadmin = new ApplicationUser
        {
            UserName = superadminEmail,
            Email = superadminEmail,
            EmailConfirmed = true,
            FullName = "Superadmin",
            Role = UserRole.Superadmin,
            IsActive = true,
            Balance = 0,
        };

        var result = await userManager.CreateAsync(superadmin, superadminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(superadmin, RoleNames.Superadmin);
        }
    }
}

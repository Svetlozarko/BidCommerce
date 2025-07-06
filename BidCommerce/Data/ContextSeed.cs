using BidCommerce.Enums;
using Microsoft.AspNetCore.Identity;

namespace BidCommerce.Data
{
    public class ContextSeed
    {
        public static async Task SeedRoleAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (string roleName in Enum.GetNames(typeof(Roles)))
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
        public static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
        {
            ApplicationUser defaultUser = new()
            {
                NickName = "Admin",
                Email = "ivan@gmail.com",
                FirstName = "Ivan",
                LastName = "Ivanov",
                Country = "defaultCountry",
                PhoneNumber = "0888888888",
                IsSeller = true,
                Age = 30,
                EmailConfirmed = true

            };

            ApplicationUser foundUser = await userManager.FindByEmailAsync(defaultUser.Email);

            if (foundUser == null)
            {
                await userManager.CreateAsync(defaultUser, "1234Ivan!");

                await userManager.AddToRoleAsync(defaultUser, Roles.Admin.ToString());
            }

        }
    }
}
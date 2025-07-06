using BidCommerce.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BidCommerce.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleRole(string role)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found" });

            if (role == "Seller")
            {
                if (!await _userManager.IsInRoleAsync(user, "Seller"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Buyer");
                    await _userManager.AddToRoleAsync(user, "Seller");
                    user.IsSeller = true;
                }
            }
            else if (role == "Buyer")
            {
                if (!await _userManager.IsInRoleAsync(user, "Buyer"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Seller");
                    await _userManager.AddToRoleAsync(user, "Buyer");
                    user.IsSeller = false;
                }
            }

            await _userManager.UpdateAsync(user);

            return Json(new { success = true, newRole = role });
        }




    }

}

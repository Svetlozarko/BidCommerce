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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ToggleRole(string role)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index", "Home");

            if (role == "Seller")
            {
                if (!await _userManager.IsInRoleAsync(user, "Seller"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Buyer");
                    await _userManager.AddToRoleAsync(user, "Seller");
                }
            }
            else if (role == "Buyer")
            {
                if (!await _userManager.IsInRoleAsync(user, "Buyer"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Seller");
                    await _userManager.AddToRoleAsync(user, "Buyer");
                }
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }


    }

}

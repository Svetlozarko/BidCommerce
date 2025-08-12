using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Stripe;
using Microsoft.Extensions.Options;

namespace BidCommerce.Controllers
{
    [Authorize]
    public class SellerOnboardingController : Controller
    {
        private readonly StripeSettings _stripeSettings;

        public SellerOnboardingController(IOptions<StripeSettings> stripeSettings)
        {
            _stripeSettings = stripeSettings.Value;
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user already has a connected Stripe account
            var userId = User.Identity.Name; // Adjust based on your user system
            var hasStripeAccount = await CheckUserHasStripeAccount(userId);

            if (hasStripeAccount)
            {
                return RedirectToAction("Create", "Product");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateStripeAccount()
        {
            try
            {
                var userId = User.Identity.Name; // Adjust based on your user system
                var userEmail = User.FindFirst("email")?.Value; // Adjust based on your claims

                var options = new AccountCreateOptions
                {
                    Type = "express",
                    Country = "US", // Adjust based on your requirements
                    Email = userEmail,
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        CardPayments = new AccountCapabilitiesCardPaymentsOptions
                        {
                            Requested = true,
                        },
                        Transfers = new AccountCapabilitiesTransfersOptions
                        {
                            Requested = true,
                        },
                    },
                    BusinessType = "individual", // or "company"
                };

                var service = new AccountService();
                var account = await service.CreateAsync(options);

                // Save the Stripe account ID to your database
                await SaveStripeAccountId(userId, account.Id);

                // Create account link for onboarding
                var linkOptions = new AccountLinkCreateOptions
                {
                    Account = account.Id,
                    RefreshUrl = Url.Action("Index", "SellerOnboarding", null, Request.Scheme),
                    ReturnUrl = Url.Action("OnboardingComplete", "SellerOnboarding", null, Request.Scheme),
                    Type = "account_onboarding",
                };

                var linkService = new AccountLinkService();
                var accountLink = await linkService.CreateAsync(linkOptions);

                return Json(new { success = true, url = accountLink.Url });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnboardingComplete()
        {
            var userId = User.Identity.Name;
            var stripeAccountId = await GetUserStripeAccountId(userId);

            if (!string.IsNullOrEmpty(stripeAccountId))
            {
                // Verify the account is properly set up
                var service = new AccountService();
                var account = await service.GetAsync(stripeAccountId);

                if (account.ChargesEnabled && account.PayoutsEnabled)
                {
                    TempData["SuccessMessage"] = "Your Stripe account has been successfully connected! You can now start selling.";
                    return RedirectToAction("Create", "Product");
                }
            }

            TempData["ErrorMessage"] = "There was an issue completing your Stripe account setup. Please try again.";
            return RedirectToAction("Index");
        }

        private async Task<bool> CheckUserHasStripeAccount(string userId)
        {
            // Implement your database logic to check if user has a connected Stripe account
            // Return true if user has a valid, active Stripe account
            return false; // Placeholder
        }

        private async Task SaveStripeAccountId(string userId, string stripeAccountId)
        {
            // Implement your database logic to save the Stripe account ID
            // Associate it with the user
        }

        private async Task<string> GetUserStripeAccountId(string userId)
        {
            // Implement your database logic to retrieve the user's Stripe account ID
            return null; // Placeholder
        }
    }

    public class StripeSettings
    {
        public string PublishableKey { get; set; }
        public string SecretKey { get; set; }
    }
}

using BidCommerce.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace BidCommerce.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly BidDb _context;

        public PaymentController(IConfiguration configuration, BidDb context)
        {
            _context = context;
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession(int productId, decimal price, string productName, string productImage)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(price * 100), // Convert to cents
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = productName,
                                    Images = new List<string> { productImage },
                                },
                            },
                            Quantity = 1,
                        },
                    },
                    Mode = "payment",
                    SuccessUrl = Url.Action("Success", "Payment", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = Url.Action("Cancel", "Payment", null, Request.Scheme),
                    Metadata = new Dictionary<string, string>
                    {
                        {"product_id", productId.ToString()},
                        {"buyer_id", User.Identity.Name ?? "guest"}
                    }
                };

                var service = new SessionService();
                Session session = await service.CreateAsync(options);

                return Json(new { url = session.Url });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> Success(string session_id)
        {
            var service = new SessionService();
            var session = await service.GetAsync(session_id);

            if (session.PaymentStatus == "paid")
            {
                // TODO: Update your database with the successful payment
                // Mark product as sold, create order record, etc.

                ViewBag.SessionId = session_id;
                ViewBag.CustomerEmail = session.CustomerDetails?.Email;
                ViewBag.AmountTotal = session.AmountTotal / 100.0; // Convert from cents

                return View();
            }

            return RedirectToAction("Cancel");
        }

        public IActionResult Cancel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"]
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    // Handle successful payment
                    // Update database, send emails, etc.
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Webhook error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> RedirectToCheckout(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound("Product not found.");

            if (product.BuyNowPrice == null)
                return BadRequest("This product does not have a 'Buy Now' price.");

            if (string.IsNullOrWhiteSpace(product.ImageUrl))
                return BadRequest("Product does not have an image.");

            if (string.IsNullOrEmpty(product.Owner?.StripeAccountId))
                return View("SellerStripeError");
            string productImageRelative = Url.Content($"~/{product.ImageUrl.TrimStart('/')}");
            string absoluteImageUrl = $"{Request.Scheme}://{Request.Host}{productImageRelative}";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(product.BuyNowPrice.Value * 100), // Convert to cents
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = product.Title,
                        Images = new List<string> { absoluteImageUrl },
                    },
                },
                Quantity = 1,
            },
        },
                Mode = "payment",
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    TransferData = new SessionPaymentIntentDataTransferDataOptions
                    {
                        Destination = product.Owner.StripeAccountId 
                    }
                },
                SuccessUrl = Url.Action("Success", "Payment", null, Request.Scheme)
                             + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = Url.Action("Cancel", "Payment", null, Request.Scheme),
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

    }
}

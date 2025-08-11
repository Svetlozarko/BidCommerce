using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using BidCommerce.Models;
using BidCommerce.Data;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly BidDb _dbContext;

    public PaymentController(BidDb dbContext)
    {
        _dbContext = dbContext;
        StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
    }

    public class PaymentRequest
    {
        public long Amount { get; set; }    
        public string Currency { get; set; } = "usd";
        public string PaymentMethodId { get; set; }
        public string Description { get; set; }
    }

    [HttpPost("pay")]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        if (request == null || request.Amount <= 0 || string.IsNullOrEmpty(request.PaymentMethodId))
            return BadRequest(new { error = "Invalid payment request." });

        // Step 1: Create Order record (status pending)
        var order = new Order
        {
            BuyerId = User?.Identity?.Name ?? "guest",
            Amount = request.Amount,
            Currency = request.Currency,
            Description = request.Description,
            PaymentStatus = PaymentStatus.Pending
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var options = new PaymentIntentCreateOptions
        {
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethod = request.PaymentMethodId,
            Description = request.Description,
            Confirm = true,
            Metadata = new System.Collections.Generic.Dictionary<string, string>
            {
                { "order_id", order.Id.ToString() },
                { "user_id", order.BuyerId }
            }
        };

        var service = new PaymentIntentService();

        try
        {
            var paymentIntent = await service.CreateAsync(options);

            // Step 3: Update order with payment info
            order.PaymentIntentId = paymentIntent.Id;
            order.PaymentStatus = MapStripeStatus(paymentIntent.Status);
            _dbContext.Orders.Update(order);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                paymentIntentId = paymentIntent.Id,
                status = paymentIntent.Status,
                orderId = order.Id
            });
        }
        catch (StripeException e)
        {
            order.PaymentStatus = PaymentStatus.Failed;
            _dbContext.Orders.Update(order);
            await _dbContext.SaveChangesAsync();

            return BadRequest(new
            {
                success = false,
                error = e.StripeError?.Message ?? e.Message
            });
        }
    }

    private PaymentStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus?.ToLower() switch
        {
            "succeeded" => PaymentStatus.Succeeded,
            "requires_action" => PaymentStatus.RequiresAction,
            "canceled" => PaymentStatus.Canceled,
            "processing" => PaymentStatus.Pending,
            _ => PaymentStatus.Failed,
        };
    }

}

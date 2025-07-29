using BidCommerce.Data;
using Microsoft.EntityFrameworkCore;

namespace BidCommerce.Services
{
    public class ExpiredProductsCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpiredProductsCleanupService> _logger;

        public ExpiredProductsCleanupService(IServiceScopeFactory scopeFactory, ILogger<ExpiredProductsCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExpiredProductsCleanupService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<BidDb>();

                    var now = DateTime.UtcNow;

                    var expiredProducts = await context.Products
                        .Where(p => p.StatusId == 2 && p.BidEndTime.HasValue && p.BidEndTime <= now)
                        .ToListAsync(stoppingToken);


                    var Nnow = DateTime.UtcNow;
                    _logger.LogInformation($"Now is {Nnow:o}");

                    var candidates = await context.Products
                        .Where(p => p.StatusId == 2 && p.BidEndTime.HasValue)
                        .ToListAsync(stoppingToken);

                    foreach (var product in candidates)
                    {
                        _logger.LogInformation($"Product Id {product.Id} BidEndTime={product.BidEndTime:o}");
                    }

                    var expired = candidates.Where(p => p.BidEndTime <= now).ToList();
                    _logger.LogInformation($"Expired products count: {expired.Count}");



                    if (expiredProducts.Any())
                    {
                        foreach (var product in expiredProducts)
                        {
                            product.StatusId = 5; 
                        }

                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"{expiredProducts.Count} expired products marked as StatusId=5.");
                    }
                    else { 
                        _logger.LogInformation("No expired products found.");}
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during expired products cleanup.");
                }

                // Run every 10 minutes (adjust as needed)
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("ExpiredProductsCleanupService stopped.");
        }
    }

}

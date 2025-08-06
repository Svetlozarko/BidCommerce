using StackExchange.Redis;
using BidCommerce.Interfaces;

namespace BidCommerce.Services
{
    public class SearchableTextService : ISearchableTextRedis
    {
        private readonly IDatabase _redis;
        private readonly ILogger<SearchableTextService> _logger;

        public SearchableTextService(IConnectionMultiplexer redis, ILogger<SearchableTextService> logger)
        {
            _redis = redis.GetDatabase();
            _logger = logger;
        }

        public async Task SaveProductAsync(int productId, string searchableText)
        {
            string cleanedText = searchableText
                .Replace("\r", " ")
                .Replace("\n", " ");
            string key = $"product:{productId}";
            await _redis.StringSetAsync(key, cleanedText);
        }


        public async Task<string?> GetProductAsync(int productId)
        {
            string key = $"product:{productId}";
            return await _redis.StringGetAsync(key);
        }

        public async Task DeleteProductAsync(int productId)
        {
            string key = $"product:{productId}";
            await _redis.KeyDeleteAsync(key);
        }
    }

}

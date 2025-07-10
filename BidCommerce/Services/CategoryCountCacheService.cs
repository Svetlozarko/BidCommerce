using BidCommerce.Interfaces;
using StackExchange.Redis;

namespace BidCommerce.Services
{
    public class CategoryCountCacheService : ICategoryCountCacheService
    {
        private readonly IDatabase _redisDb;

        public CategoryCountCacheService(IConnectionMultiplexer connection)
        {
            _redisDb = connection.GetDatabase();
        }

        public async Task<int?> GetCategoryCountAsync(string category)
        {
            var value = await _redisDb.StringGetAsync($"category_count:{category}");
            return value.HasValue ? (int?)int.Parse(value!) : null;
        }

        public async Task SetCategoryCountAsync(string category, int count, TimeSpan? expiry = null)
        {
            await _redisDb.StringSetAsync($"category_count:{category}", count, expiry);
        }
    }

}

namespace BidCommerce.Interfaces
{
    public interface ICategoryCountCacheService
    {
        Task<int?> GetCategoryCountAsync(string category);
        Task SetCategoryCountAsync(string category, int count, TimeSpan? expiry = null);
    }

}

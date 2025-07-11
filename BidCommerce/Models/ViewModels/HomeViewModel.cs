using BidCommerce.Models;

namespace BidCommerce.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Product> RecentProducts { get; set; } = new List<Product>();
    }
}

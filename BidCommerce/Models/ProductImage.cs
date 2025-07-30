using System.ComponentModel.DataAnnotations.Schema;

namespace BidCommerce.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public string ImageUrl { get; set; }

        public bool IsPrimary { get; set; }
        public int? DisplayOrder { get; set; } // Optional: for ordering images

    }

}

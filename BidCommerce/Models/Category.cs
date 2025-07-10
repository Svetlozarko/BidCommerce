using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BidCommerce.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public virtual ICollection<Product> Products { get; set; }

        [NotMapped]
        public int ItemCount { get; set; }
    }

}

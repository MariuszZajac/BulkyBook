using System.ComponentModel.DataAnnotations;

namespace BulkyBook.Models
{
    public class ShoppingCart
    {
        public Product Product { get; set; }
        [Range(1,1000, ErrorMessage = "Please enter value between 1-1000")]
        public int Count { get; set; }
    }
}

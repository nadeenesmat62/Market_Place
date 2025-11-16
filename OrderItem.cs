using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JWTRefreshTokenInDotNet6.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("OrderId")]
        public int OrderId { get; set; }
        public  Order Order { get; set; }
        [ForeignKey("ProductId")]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [ForeignKey("VendorId")]
        public string VendorId { get; set; }
        public Vendor Vendor { get; set; }

        public int Quantity { get; set; }
        [Precision(18, 2)]
        public decimal Price { get; set; }
        //public required ICollection<Product> Products { get; set; }

    }
}

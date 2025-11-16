using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTRefreshTokenInDotNet6.Models;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string VendorId { get; set; }

    [Required, ForeignKey("VendorId")]
    public virtual Vendor Vendor { get; set; }

    [Required, StringLength(50)]
    public String VendorName { get; set; }

    [Required, StringLength(50)]
    public string Title { get; set; }

    [Required, StringLength(250)]
    public string Description { get; set; }

    [Required]
    public string Category { get; set; }

    [Required]
    public double Price { get; set; }

    [Required]
    public string ImageUrl { get; set; }

    [Required]
    public int NumOfUnits { get; set; }

    // public List<Customer>? CustomersWhoPurchase { get; set; }

    public bool Isapproved { get; set; } = false;

    public int NumOfViews { get; set; } = 0;

    public List<Favorite> Favorites { get; set; }

    public List<CartItem> CartItems { get; set; }
    public List<OrderItem> OrderItems { get; set; }

    public List<ProductViewNum> ProductViews { get; set; }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace JWTRefreshTokenInDotNet6.Models;

public class Customer : ApplicationUser
{
    public virtual List<CreditCard>? CreditCards { get; set; } = new();

    //  public List<Product>? Products { get; set; }
    public virtual Favorite Favorite { get; set; } = new();
    public virtual Cart Cart { get; set; } = new();
    public virtual List<Order> Orders { get; set; } = new();
    public List<ProductViewNum> ProductViews { get; set; }
}

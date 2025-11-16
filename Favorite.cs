using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTRefreshTokenInDotNet6.Models
{
    public class Favorite
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("CustomerId")]
        public string CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();
    }
}
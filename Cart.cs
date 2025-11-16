using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JWTRefreshTokenInDotNet6.Models
{
    public class Cart
    {

        [Key]
        public int Id { get; set; }
        [ForeignKey("CustomerId")]
        public string CustomerId { get; set; }
        public Customer Customer { get; set; }
        public List<CartItem> CartItems { get; set; }
    }
}
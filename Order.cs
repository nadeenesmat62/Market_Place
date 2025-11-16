using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using JWTRefreshTokenInDotNet6.Models;
using Microsoft.EntityFrameworkCore;
namespace JWTRefreshTokenInDotNet6.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("CustomerId")]
        public string CustomerId { get; set; }
        public Customer Customer { get; set; }
        [Precision(18, 2)]
        public decimal TotalPrice { get; set; }
        public List<OrderItem> OrderItems { get; set; }

    }
}

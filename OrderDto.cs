using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JWTRefreshTokenInDotNet6.Models
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
    }
}
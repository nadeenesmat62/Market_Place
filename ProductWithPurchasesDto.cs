using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JWTRefreshTokenInDotNet6.Models
{
    public class ProductWithPurchasesDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public List<CustomerPurchaseInfoDto> Purchases { get; set; }
    }
}
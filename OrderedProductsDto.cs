using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JWTRefreshTokenInDotNet6.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTRefreshTokenInDotNet6.Models;

public class OrderedProductDto
{
    public string ProductTitle { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

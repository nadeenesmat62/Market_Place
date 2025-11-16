using JWTRefreshTokenInDotNet6.Models;
using System.ComponentModel.DataAnnotations.Schema;
namespace JWTRefreshTokenInDotNet6.Models;

public class Vendor :ApplicationUser
{
   public List<Product>? products {get; set;}

}

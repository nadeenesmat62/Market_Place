using JWTRefreshTokenInDotNet6.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JWTRefreshTokenInDotNet6.Models;

public class ProductDto
{  [Required,StringLength(250)]
     public string Title { get; set; }
      [Required,StringLength(250)]
        public string Description { get; set; }
         [Required,StringLength(50)]
        public string Category { get; set; }
         [Required]
        public double Price { get; set; }
        public string? ImageUrl { get; set; }
         [Required]
        public int NumOfUnits { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JWTRefreshTokenInDotNet6.Models
{
    public class AddProductRequest
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
       
        [ForeignKey("ProductId")]
     public virtual Product Product { get; set; }
     public string VendorId { get; set; } 


    public Status status {get; set;}=Status.Pending;
    }
     public enum Status
{
    Pending,
    Approved,
    Rejected
}
}

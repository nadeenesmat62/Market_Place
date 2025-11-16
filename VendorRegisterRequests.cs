using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JWTRefreshTokenInDotNet6.Models;
public class VendorRegisterRequest
{   
    [Key]
    public int Id { get; set; }
    public string VendorId { get; set; } 
    public string VendorUsername { get; set; }
    public string VendorEmail { get; set; }

    public VendorStatus status {get; set;}=VendorStatus.Pending;
    }
     public enum VendorStatus
{
    Pending,
    Approved,
    Rejected
}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace JWTRefreshTokenInDotNet6.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(50)]
        public string FirstName { get; set; }

        [MaxLength(50)]
        public string LastName { get; set; }
        public bool AutoPublish { get; set; } = false;
        public bool CanEdit { get; set; } = true; // انا غيرتها ل true  متنسيش تقولي ل يوسف
        public bool CanDelete { get; set; } = true; // for all indevidual vendors
        public bool CanRegister { get; set; } = true;
        public List<RefreshToken>? RefreshTokens { get; set; }
    }
}

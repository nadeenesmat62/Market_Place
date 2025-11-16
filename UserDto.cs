using System.ComponentModel.DataAnnotations;

namespace JWTRefreshTokenInDotNet6.Models
{
    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public Boolean AutoPublish { get; set; }
        public Boolean CanEdit { get; set; }
        public Boolean CanDelete { get; set; }
    }
}

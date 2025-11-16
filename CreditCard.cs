using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JWTRefreshTokenInDotNet6.Helpers;

namespace JWTRefreshTokenInDotNet6.Models
{
    public class CreditCard
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        public string CardName {get; set;}

        [Required]
        public string EncryptedCardNumber { get; set; }

        [Required]
        public string CardType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExpirDate { get; set; }
    }
}

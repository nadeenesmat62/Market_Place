using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JWTRefreshTokenInDotNet6.Models;
using Microsoft.EntityFrameworkCore;

namespace JWTRefreshTokenInDotNet6.Models;

public class PaymentDto
{
    public string CardName { get; set; }
    public required string NumOfCard { get; set; }
    public required string CardType { get; set; }
    public DateTime ExpirDate { get; set; }
}

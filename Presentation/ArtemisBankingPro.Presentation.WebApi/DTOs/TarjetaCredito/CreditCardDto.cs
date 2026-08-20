namespace ArtemisBankingPro.Presentation.WebApi.DTOs;

public class CreditCardDto
{
    public string Id { get; set; } = null!;
    public string MaskedCardNumber { get; set; } = null!;
    public string LastFourDigits { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientFullName { get; set; } = null!;
    public decimal CreditLimit { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal CurrentDebt { get; set; }
    public string ExpirationDate { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
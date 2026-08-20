namespace ArtemisBankingPro.Presentation.WebApi.DTOs;

public class AssignCreditCardRequest
{
    public string ClientId { get; set; } = null!;
    public decimal CreditLimit { get; set; }
}
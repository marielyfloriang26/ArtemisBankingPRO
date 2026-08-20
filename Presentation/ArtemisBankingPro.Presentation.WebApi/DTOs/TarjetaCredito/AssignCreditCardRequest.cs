using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Presentation.WebApi.DTOs;

public class AssignCreditCardRequest
{
    [Required(ErrorMessage = "El clientId es requerido.")]
    public string ClientId { get; set; } = null!;

    [Range(0.01, double.MaxValue, ErrorMessage = "El límite debe ser mayor que cero.")]
    public decimal CreditLimit { get; set; }
}
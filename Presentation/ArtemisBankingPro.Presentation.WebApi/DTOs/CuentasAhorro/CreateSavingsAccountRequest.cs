using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Presentation.WebApi.DTOs.CuentasAhorro;

public class CreateSavingsAccountRequest
{
    [Required(ErrorMessage = "El clientId es requerido.")]
    public string ClientId { get; set; } = null!;

    [Required(ErrorMessage = "El initialBalance es requerido.")]
    [Range(0, double.MaxValue, ErrorMessage = "El balance inicial no puede ser negativo.")]
    public decimal InitialBalance { get; set; }
}
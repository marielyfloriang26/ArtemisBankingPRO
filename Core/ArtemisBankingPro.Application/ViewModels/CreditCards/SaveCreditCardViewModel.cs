using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.CreditCards;

public class SaveCreditCardViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la tarjeta es requerido.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El límite de crédito es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El límite de crédito debe ser mayor a 0.")]
    public decimal LimiteCredito { get; set; }

    [Required(ErrorMessage = "La tasa de interés es requerida.")]
    [Range(0.01, 100, ErrorMessage = "La tasa de interés debe estar entre 0.01 y 100.")]
    public decimal TasaInteres { get; set; }

    public decimal CostoEmision { get; set; } = 0.00m;

    [MaxLength(500)]
    public string? Descripcion { get; set; }
}

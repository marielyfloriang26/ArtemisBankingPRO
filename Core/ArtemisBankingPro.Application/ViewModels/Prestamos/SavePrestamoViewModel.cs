using System;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Prestamos;

public class SavePrestamoViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El cliente seleccionado es requerido.")]
    public int ClienteId { get; set; }

    [Required(ErrorMessage = "El monto a prestar debe ser mayor que cero.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a prestar debe ser mayor que cero.")]
    public decimal MontoAprobado { get; set; }

    [Required(ErrorMessage = "El plazo seleccionado no es válido.")]
    [Range(6, 60, ErrorMessage = "El plazo seleccionado no es válido.")]
    public int PlazoMeses { get; set; }

    [Required(ErrorMessage = "La tasa de interés anual no puede ser negativa.")]
    [Range(0, 100, ErrorMessage = "La tasa de interés anual no puede ser negativa.")]
    public decimal TasaInteresAnual { get; set; }

    public int AdminId { get; set; }
}

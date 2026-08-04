using System;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Prestamos;

public class EditTasaPrestamoViewModel
{
    [Required]
    public int PrestamoId { get; set; }

    [Required(ErrorMessage = "La tasa de interés anual no puede ser negativa.")]
    [Range(0, 100, ErrorMessage = "La tasa de interés anual no puede ser negativa.")]
    public decimal NuevaTasaInteresAnual { get; set; }
}

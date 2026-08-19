using System;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.AdminTarjeta
{
    public class EditLimiteTarjetaViewModel
    {
        [Required]
        public int TarjetaId { get; set; }

        [Required(ErrorMessage = "El límite de la tarjeta debe ser mayor que cero.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El límite de la tarjeta debe ser mayor que cero.")]
        public decimal NuevoLimite { get; set; }
    }
}

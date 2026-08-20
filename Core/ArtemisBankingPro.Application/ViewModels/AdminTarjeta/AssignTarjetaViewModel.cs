using System;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.AdminTarjeta
{
    public class AssignTarjetaViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un cliente para continuar.")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "El límite de crédito debe ser mayor que cero.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El límite de crédito debe ser mayor que cero.")]
        public decimal LimiteCredito { get; set; }

        public int AdminId { get; set; }
    }
}

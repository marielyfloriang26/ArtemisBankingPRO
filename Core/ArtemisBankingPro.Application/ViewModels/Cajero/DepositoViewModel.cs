using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Cajero
{
    public class DepositoViewModel
    {
        [Required(ErrorMessage = "El número de cuenta es requerido.")]
        [Display(Name = "Número de Cuenta")]
        public string NumeroCuenta { get; set; } = null!;

        [Required(ErrorMessage = "El monto a depositar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        [Display(Name = "Monto (RD$)")]
        public decimal Monto { get; set; }
    }
}
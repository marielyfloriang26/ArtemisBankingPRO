using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Cajero
{
    public class RetiroViewModel
    {
        [Required(ErrorMessage = "El número de cuenta es requerido.")]
        [Display(Name = "Número de Cuenta")]
        public string NumeroCuenta { get; set; } = null!;

        [Required(ErrorMessage = "El monto a retirar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        [Display(Name = "Monto (RD$)")]
        public decimal Monto { get; set; }
        public int CuentaId { get; set; }
        public string? TitularCuenta { get; set; }
    }
}
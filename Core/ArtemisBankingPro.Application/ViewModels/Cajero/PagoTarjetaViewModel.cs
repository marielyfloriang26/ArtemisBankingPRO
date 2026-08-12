using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Cajero
{
    public class PagoTarjetaViewModel
    {
        [Required(ErrorMessage = "El número de tarjeta es requerido.")]
        [Display(Name = "Número de Tarjeta")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "La tarjeta debe tener 16 dígitos.")]
        public string NumeroTarjeta { get; set; } = null!;

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        [Display(Name = "Monto (RD$)")]
        public decimal Monto { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Cajero
{
    public class PagoTarjetaViewModel
    {
        [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
        [Display(Name = "Número de Cuenta Origen")]
        public string NumeroCuenta { get; set; } = null!;

        [Required(ErrorMessage = "El número de tarjeta es requerido.")]
        [Display(Name = "Número de Tarjeta")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "La tarjeta debe tener 16 dígitos.")]
        public string NumeroTarjeta { get; set; } = null!;

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        [Display(Name = "Monto (RD$)")]
        public decimal Monto { get; set; }

        // Campos usados solo para mostrar en la pantalla de confirmación
        public int CuentaOrigenId { get; set; }
        public string? TitularCuentaOrigen { get; set; }
        public int TarjetaId { get; set; }
        public string? TitularTarjeta { get; set; }
        public decimal MontoEfectivo { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Cliente
{
    public class TransferenciaViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar una cuenta de origen.")]
        [Display(Name = "Cuenta de Ahorro Origen")]
        public int CuentaOrigenId { get; set; }

        [Required(ErrorMessage = "Debe ingresar el número de cuenta de destino.")]
        [Display(Name = "Número de Cuenta Destino")]
        public string NumeroCuentaDestino { get; set; } = null!;

        [Required(ErrorMessage = "El monto a transferir es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        [Display(Name = "Monto a Transferir (RD$)")]
        public decimal Monto { get; set; }
    }
}
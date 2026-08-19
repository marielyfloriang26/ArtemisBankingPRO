using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Cliente
{
    public class AvanceEfectivoViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar una tarjeta de crédito de origen.")]
        [Display(Name = "Tarjeta de Crédito Origen")]
        public int TarjetaCreditoId { get; set; } // ID o num de la tarjeta

        [Required(ErrorMessage = "Debe seleccionar una cuenta de ahorro de destino.")]
        [Display(Name = "Cuenta de Ahorro Destino")]
        public int CuentaAhorroDestinoId { get; set; } // ID o num de la cuenta

        [Required(ErrorMessage = "El monto del avance es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto del avance debe ser un valor numérico mayor que cero.")]
        [Display(Name = "Monto del Avance (RD$)")]
        public decimal Monto { get; set; }
    }
}
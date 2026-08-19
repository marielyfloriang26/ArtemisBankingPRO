using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.AdminTarjeta
{
    public class TarjetaAdminViewModel
    {
        public int Id { get; set; }
        public string NumeroTarjeta { get; set; }
        public string Cliente { get; set; }
        public decimal LimiteCredito { get; set; }
        public string FechaExpiracion { get; set; }
        public decimal MontoAdeudado { get; set; }
        public string Estado { get; set; }
    }
}

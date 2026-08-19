using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.AdminTarjeta
{
    public class TarjetaAdminViewModel
    {
        public int Id { get; set; }
        public string NumeroTarjeta { get; set; } = null!;
        public string Cliente { get; set; } = null!;
        public string Cedula { get; set; } = null!;
        public decimal LimiteCredito { get; set; }
        public string FechaExpiracion { get; set; } = null!;
        public decimal MontoAdeudado { get; set; }
        public string Estado { get; set; } = null!;
    }
}

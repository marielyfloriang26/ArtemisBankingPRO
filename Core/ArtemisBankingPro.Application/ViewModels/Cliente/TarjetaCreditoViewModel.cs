using System;

namespace ArtemisBankingPro.Application.ViewModels.Cliente
{
    public class TarjetaCreditoViewModel
    {
        public int Id { get; set; }
        public string NumeroTarjeta { get; set; } = null!;
        public decimal LimiteCredito { get; set; }
        public decimal MontoAdeudado { get; set; }
        public string FechaExpiracion { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public int ClienteId { get; set; }

        // Propiedad de conveniencia para mostrar en el dropdown del formulario
        public string DescripcionDropdown => $"Tarjeta terminada en {NumeroTarjeta.Substring(Math.Max(0, NumeroTarjeta.Length - 4))} (Disponible: RD$ {(LimiteCredito - MontoAdeudado):N2})";
    }
}
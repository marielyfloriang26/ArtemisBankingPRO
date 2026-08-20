using System;

namespace ArtemisBankingPro.Application.ViewModels.AdminTarjeta
{
    public class ConsumoTarjetaViewModel
    {
        public DateTime FechaConsumo { get; set; }
        public decimal MontoConsumido { get; set; }
        public string Comercio { get; set; } = null!;
        public string EstadoConsumo { get; set; } = null!;
    }
}

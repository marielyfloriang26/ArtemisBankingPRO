using System;

namespace ArtemisBankingPro.Application.ViewModels.AdminTarjeta
{
    public class ConsumoTarjetaViewModel
    {
        public DateTime FechaConsumo { get; set; }
        public decimal MontoConsumido { get; set; }
        public string Comercio { get; set; }
        public string EstadoConsumo { get; set; }
    }
}

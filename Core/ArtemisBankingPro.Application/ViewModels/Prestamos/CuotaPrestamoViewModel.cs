using System;

namespace ArtemisBankingPro.Application.ViewModels.Prestamos;

public class CuotaPrestamoViewModel
{
    public int Id { get; set; }
    public int PrestamoId { get; set; }
    public int NumeroCuota { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal ValorCuota { get; set; }
    public decimal MontoInteres { get; set; }
    public decimal MontoCapital { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string EstadoPago { get; set; } = null!;
    public bool TieneAtraso { get; set; }
}

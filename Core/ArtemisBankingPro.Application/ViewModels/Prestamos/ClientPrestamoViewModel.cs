using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Application.ViewModels.Prestamos;

public class ClientPrestamoViewModel
{
    public int Id { get; set; }
    public string NumeroPrestamo { get; set; } = null!;
    public decimal MontoAprobado { get; set; }
    public decimal TasaInteresAnual { get; set; }
    public int PlazoMeses { get; set; }
    public decimal MontoPendiente { get; set; }
    public decimal ValorProximaCuota { get; set; }
    public DateTime? FechaVencimientoProximaCuota { get; set; }
    public string Estado { get; set; } = null!;
}

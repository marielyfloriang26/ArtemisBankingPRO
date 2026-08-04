using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Application.ViewModels.Prestamos;

public class PrestamoViewModel
{
    public int Id { get; set; }
    public string NumeroPrestamo { get; set; } = null!;
    public int ClienteId { get; set; }
    public string NombreCliente { get; set; } = null!;
    public decimal MontoAprobado { get; set; }
    public decimal TasaInteresAnual { get; set; }
    public int PlazoMeses { get; set; }
    public decimal MontoPendiente { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
    public List<CuotaPrestamoViewModel> Cuotas { get; set; } = new();

    public string EstadoCliente => Cuotas.Any(c => c.TieneAtraso && c.EstadoPago != "Pagada") ? "En mora" : "Al día";
}

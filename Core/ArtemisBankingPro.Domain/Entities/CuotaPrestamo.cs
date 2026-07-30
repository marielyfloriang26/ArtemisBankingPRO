using System;

namespace ArtemisBankingPro.Domain.Entities;

public class CuotaPrestamo
{
    public int Id { get; set; }
    public int PrestamoId { get; set; }
    public int NumeroCuota { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal ValorCuota { get; set; } // Capital + Interés
    public decimal MontoInteres { get; set; }
    public decimal MontoCapital { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string EstadoPago { get; set; } = "Pendiente"; // Pendiente, ParcialmentePagada, Pagada
    public bool TieneAtraso { get; set; } = false;

    // Propiedades de navegación
    public Prestamo? Prestamo { get; set; }
}

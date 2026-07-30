using System;

namespace ArtemisBankingPro.Domain.Entities;

public class ConsumoTarjeta
{
    public int Id { get; set; }
    public int TarjetaId { get; set; }
    public decimal Monto { get; set; }
    public string Comercio { get; set; } = null!; // Nombre o "AVANCE"
    public string Estado { get; set; } = null!; // APROBADO o RECHAZADO
    public DateTime FechaConsumo { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación
    public TarjetaCredito? Tarjeta { get; set; }
}

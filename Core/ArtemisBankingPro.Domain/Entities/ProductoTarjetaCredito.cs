using System;

namespace ArtemisBankingPro.Domain.Entities;

public class ProductoTarjetaCredito
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal LimiteCredito { get; set; }
    public decimal TasaInteres { get; set; }
    public decimal CostoEmision { get; set; } = 0.00m;
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = "Activa"; // Activa o Inactiva
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

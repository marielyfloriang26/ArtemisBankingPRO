using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Domain.Entities;

public class TarjetaCredito
{
    public int Id { get; set; }
    public string NumeroTarjeta { get; set; } = null!; // Identificador de 16 dígitos únicos
    public int ClienteId { get; set; }
    public decimal LimiteCredito { get; set; }
    public decimal MontoAdeudado { get; set; } = 0.00m;
    public string FechaExpiracion { get; set; } = null!; // MM/AA
    public string CVC { get; set; } = null!; // Hash SHA-256
    public string Estado { get; set; } = "Activa"; // Activa o Cancelada
    public int AdminId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    // Propiedades de navegación
    public Usuario? Cliente { get; set; }
    public Usuario? Admin { get; set; }
    public ICollection<ConsumoTarjeta>? Consumos { get; set; }
}

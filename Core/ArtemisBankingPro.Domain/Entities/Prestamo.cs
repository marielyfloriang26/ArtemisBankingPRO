using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Domain.Entities;

public class Prestamo
{
    public int Id { get; set; }
    public string NumeroPrestamo { get; set; } = null!; // Identificador de 9 dígitos únicos
    public int ClienteId { get; set; }
    public decimal MontoAprobado { get; set; }
    public decimal TasaInteresAnual { get; set; }
    public int PlazoMeses { get; set; } // Múltiplos de 6 hasta 60
    public decimal MontoPendiente { get; set; }
    public string Estado { get; set; } = "Activo"; // Activo o Completado
    public int AdminId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación
    public Usuario? Cliente { get; set; }
    public Usuario? Admin { get; set; }
    public ICollection<CuotaPrestamo>? Cuotas { get; set; }
}

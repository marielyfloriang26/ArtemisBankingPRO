using System;

namespace ArtemisBankingPro.Domain.Entities;

public class Beneficiario
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int CuentaAhorroId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación
    public Usuario? Cliente { get; set; }
    public CuentaAhorro? CuentaAhorro { get; set; }
}

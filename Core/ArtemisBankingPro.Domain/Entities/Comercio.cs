using System;

namespace ArtemisBankingPro.Domain.Entities;

public class Comercio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string Correo { get; set; } = null!; // Unique
    public string Telefono { get; set; } = null!;
    public string RNC { get; set; } = null!; // Unique
    public bool EsActivo { get; set; } = true;
    public int AdminId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación
    public Usuario? Admin { get; set; }
    public ComercioUsuario? ComercioUsuarioRel { get; set; }
}

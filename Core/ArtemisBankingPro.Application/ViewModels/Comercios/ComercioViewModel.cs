using System;

namespace ArtemisBankingPro.Application.ViewModels.Comercios;

public class ComercioViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string Correo { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string RNC { get; set; } = null!;
    public bool EsActivo { get; set; }
    public bool TieneUsuarioAsociado { get; set; }
    public DateTime FechaCreacion { get; set; }
}

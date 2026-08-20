using System;

namespace ArtemisBankingPro.Application.ViewModels.Comercios;

public class ComercioDetalleViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string Correo { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string RNC { get; set; } = null!;
    public bool EsActivo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public UsuarioAsociadoViewModel? UsuarioAsociado { get; set; }
}

public class UsuarioAsociadoViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public bool EsActivo { get; set; }
}

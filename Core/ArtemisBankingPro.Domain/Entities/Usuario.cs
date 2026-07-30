using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Domain.Entities;

public class Usuario : IdentityUser<int>
{
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string Cedula { get; set; } = null!;
    public string? Telefono { get; set; }
    public string? FotoUrl { get; set; }
    public string TipoUsuario { get; set; } = null!; // Administrador, Cajero, Cliente, Comercio
    public bool EsActivo { get; set; } = false;

    // Propiedades de navegación
    public ICollection<CuentaAhorro>? CuentasAhorro { get; set; }
    public ICollection<Prestamo>? PrestamosCliente { get; set; }
    public ICollection<Prestamo>? PrestamosAutorizados { get; set; }
    public ICollection<TarjetaCredito>? TarjetasCliente { get; set; }
    public ICollection<TarjetaCredito>? TarjetasAutorizadas { get; set; }
    public ICollection<Comercio>? ComerciosRegistrados { get; set; }
    public ICollection<Beneficiario>? Beneficiarios { get; set; }
    public ICollection<Transaccion>? TransaccionesIniciadas { get; set; }
}

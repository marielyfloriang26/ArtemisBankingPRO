using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Domain.Entities;

public class CuentaAhorro
{
    public int Id { get; set; }
    public string NumeroCuenta { get; set; } = null!; // Identificador de 9 dígitos únicos
    public int ClienteId { get; set; }
    public decimal Balance { get; set; } = 0.00m;
    public string TipoCuenta { get; set; } = null!; // Principal o Secundaria
    public string Estado { get; set; } = "Activa"; // Activa o Cancelada
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación
    public Usuario? Cliente { get; set; }
    public ICollection<Beneficiario>? BeneficiariosAsociados { get; set; }
    public ICollection<Transaccion>? TransaccionesOrigen { get; set; }
    public ICollection<Transaccion>? TransaccionesDestino { get; set; }
}

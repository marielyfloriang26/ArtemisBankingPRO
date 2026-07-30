using System;

namespace ArtemisBankingPro.Domain.Entities;

public class Transaccion
{
    public int Id { get; set; }
    public int? CuentaOrigenId { get; set; } // Null si es depósito en caja
    public int? CuentaDestinoId { get; set; } // Null si es retiro por caja
    public decimal Monto { get; set; }
    public string TipoTransaccion { get; set; } = null!; // DÉBITO o CRÉDITO
    public string Origen { get; set; } = null!; // DEPÓSITO, RETIRO, AVANCE, TRANSFERENCIA, nº préstamo, nº tarjeta
    public string Beneficiario { get; set; } = null!; // DEPÓSITO, RETIRO, nº cuenta destino, nº préstamo, nº tarjeta
    public string Estado { get; set; } = "APROBADA"; // APROBADA o RECHAZADA
    public int? UsuarioResponsableId { get; set; } // Cajero o Cliente autenticado que inició la transacción
    public DateTime FechaTransaccion { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación
    public CuentaAhorro? CuentaOrigen { get; set; }
    public CuentaAhorro? CuentaDestino { get; set; }
    public Usuario? UsuarioResponsable { get; set; }
}

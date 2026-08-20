using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Transacciones;

public class TransaccionTercerosCajeroFormViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    [Display(Name = "Número de cuenta origen")]
    public string NumeroCuentaOrigen { get; set; } = null!;

    [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
    [Display(Name = "Número de cuenta destino")]
    public string NumeroCuentaDestino { get; set; } = null!;

    [Required(ErrorMessage = "El monto de la transacción es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto de la transacción debe ser mayor que cero.")]
    [Display(Name = "Monto de la transacción")]
    public decimal? Monto { get; set; }
}

public class TransaccionTercerosCajeroConfirmViewModel
{
    public int CuentaOrigenId { get; set; }
    public string NumeroCuentaOrigen { get; set; } = null!;
    public string TitularCuentaOrigen { get; set; } = null!;

    public int CuentaDestinoId { get; set; }
    public string NumeroCuentaDestino { get; set; } = null!;
    public string TitularCuentaDestino { get; set; } = null!;

    public decimal Monto { get; set; }
}

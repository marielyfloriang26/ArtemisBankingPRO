using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Transacciones;

public class PagoPrestamoCajeroFormViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    [Display(Name = "Número de cuenta origen")]
    public string NumeroCuentaOrigen { get; set; } = null!;

    [Required(ErrorMessage = "El número del préstamo es requerido.")]
    [Display(Name = "Número del préstamo")]
    public string NumeroPrestamo { get; set; } = null!;

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    [Display(Name = "Monto a pagar")]
    public decimal? Monto { get; set; }
}

public class PagoPrestamoCajeroConfirmViewModel
{
    public int CuentaOrigenId { get; set; }
    public string NumeroCuentaOrigen { get; set; } = null!;
    public string TitularCuentaOrigen { get; set; } = null!;

    public int PrestamoId { get; set; }
    public string NumeroPrestamo { get; set; } = null!;
    public string TitularPrestamo { get; set; } = null!;

    public decimal MontoIngresado { get; set; }
    public decimal MontoEfectivo { get; set; }
}

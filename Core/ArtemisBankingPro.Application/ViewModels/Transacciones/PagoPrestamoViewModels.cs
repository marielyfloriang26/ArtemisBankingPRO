using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Transacciones;

public class PagoPrestamoFormViewModel
{
    [Required(ErrorMessage = "El préstamo a pagar es requerido.")]
    [Display(Name = "Préstamo a pagar")]
    public int? PrestamoId { get; set; }

    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    [Display(Name = "Cuenta de origen")]
    public int? CuentaOrigenId { get; set; }

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    [Display(Name = "Monto a pagar")]
    public decimal? Monto { get; set; }

    public List<PrestamoSelectItemViewModel> Prestamos { get; set; } = new();
    public List<CuentaSelectItemViewModel> CuentasOrigen { get; set; } = new();
}

public class PagoPrestamoConfirmViewModel
{
    public int PrestamoId { get; set; }
    public int CuentaOrigenId { get; set; }
    public decimal MontoIngresado { get; set; }
    public decimal MontoEfectivo { get; set; }
    public string TitularCuentaOrigen { get; set; } = null!;
    public string NumeroCuentaOrigen { get; set; } = null!;
    public string TitularPrestamo { get; set; } = null!;
    public string NumeroPrestamo { get; set; } = null!;
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Transacciones;

public class PagoTarjetaFormViewModel
{
    [Required(ErrorMessage = "La tarjeta de crédito destino es requerida.")]
    [Display(Name = "Tarjeta de crédito destino")]
    public int? TarjetaId { get; set; }

    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    [Display(Name = "Cuenta de origen")]
    public int? CuentaOrigenId { get; set; }

    [Required(ErrorMessage = "El monto a pagar es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
    [Display(Name = "Monto a pagar")]
    public decimal? Monto { get; set; }

    public List<TarjetaSelectItemViewModel> Tarjetas { get; set; } = new();
    public List<CuentaSelectItemViewModel> CuentasOrigen { get; set; } = new();
}

public class PagoTarjetaConfirmViewModel
{
    public int TarjetaId { get; set; }
    public int CuentaOrigenId { get; set; }
    public decimal MontoIngresado { get; set; }
    public decimal MontoEfectivo { get; set; }
    public string TitularCuentaOrigen { get; set; } = null!;
    public string NumeroCuentaOrigen { get; set; } = null!;
    public string TitularTarjeta { get; set; } = null!;
    public string UltimosDigitosTarjeta { get; set; } = null!;
}

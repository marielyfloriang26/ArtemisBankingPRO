using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Transacciones;

public class ExpressFormViewModel
{
    [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
    [Display(Name = "Número de cuenta destino")]
    public string NumeroCuentaDestino { get; set; } = null!;

    [Required(ErrorMessage = "El monto a transferir es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
    [Display(Name = "Monto a transferir")]
    public decimal? Monto { get; set; }

    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    [Display(Name = "Cuenta de origen")]
    public int? CuentaOrigenId { get; set; }

    public List<CuentaSelectItemViewModel> CuentasOrigen { get; set; } = new();
}

public class ExpressConfirmViewModel
{
    public int CuentaOrigenId { get; set; }
    public string NumeroCuentaOrigen { get; set; } = null!;
    public string NumeroCuentaDestino { get; set; } = null!;
    public decimal Monto { get; set; }
    public string TitularCuentaDestino { get; set; } = null!;
}

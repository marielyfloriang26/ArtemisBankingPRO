using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Transacciones;

public class PagoBeneficiarioFormViewModel
{
    [Required(ErrorMessage = "El beneficiario es requerido.")]
    [Display(Name = "Beneficiario")]
    public int? BeneficiarioId { get; set; }

    [Required(ErrorMessage = "La cuenta de origen es requerida.")]
    [Display(Name = "Cuenta de origen")]
    public int? CuentaOrigenId { get; set; }

    [Required(ErrorMessage = "El monto a transferir es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
    [Display(Name = "Monto a transferir")]
    public decimal? Monto { get; set; }

    public List<BeneficiarioSelectItemViewModel> Beneficiarios { get; set; } = new();
    public List<CuentaSelectItemViewModel> CuentasOrigen { get; set; } = new();
}

public class PagoBeneficiarioConfirmViewModel
{
    public int BeneficiarioId { get; set; }
    public int CuentaOrigenId { get; set; }
    public string NumeroCuentaOrigen { get; set; } = null!;
    public decimal Monto { get; set; }
    public string TitularBeneficiario { get; set; } = null!;
    public string NumeroCuentaBeneficiario { get; set; } = null!;
}

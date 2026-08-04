using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Beneficiarios;

public class SaveBeneficiarioViewModel
{
    [Required(ErrorMessage = "El número de cuenta es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
    [RegularExpression("^[0-9]+$", ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
    [Display(Name = "Número de cuenta")]
    public string NumeroCuenta { get; set; } = null!;
}

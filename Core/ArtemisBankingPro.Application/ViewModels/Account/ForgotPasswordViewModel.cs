using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        public string UserName { get; set; } = null!;
    }
}
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Application.ViewModels.Account
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
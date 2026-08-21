using System.ComponentModel.DataAnnotations;
namespace ArtemisBankingPro.Application.DTOs.Account;
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "El Id de usuario es obligatorio.")]
    public string UserId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "El token es obligatorio.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe confirmar la nueva contraseña.")]
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

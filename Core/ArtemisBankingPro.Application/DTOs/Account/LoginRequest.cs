using System.ComponentModel.DataAnnotations;
namespace ArtemisBankingPro.Application.DTOs.Account;

public class LoginRequest
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}

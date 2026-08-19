using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Presentation.WebApi.DTOs;

public class ConfirmRequestDto
{
    [Required(ErrorMessage = "El token es obligatorio.")]
    public string Token { get; set; } = null!;
}

public class GetResetTokenRequestDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    public string UserName { get; set; } = null!;
}

public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = "El userId es obligatorio.")]
    public string UserId { get; set; } = null!;

    [Required(ErrorMessage = "El token es obligatorio.")]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
    public string ConfirmPassword { get; set; } = null!;
}

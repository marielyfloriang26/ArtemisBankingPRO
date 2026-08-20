using ArtemisBankingPro.Presentation.WebApi.DTOs;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Presentation.WebApi.DTOs.Account;

public class AuthenticateRequestDto
{
    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; set; } = null!;
}

public class AuthenticateResponseDto
{
    public string Jwt { get; set; } = null!;
}

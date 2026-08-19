using ArtemisBankingPro.Presentation.WebApi.DTOs;
using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.Presentation.WebApi.DTOs.Users;

public class CreateCommerceUserRequestDto
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "El apellido es requerido.")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "La cédula es requerida.")]
    public string Identification { get; set; } = null!;

    [Required(ErrorMessage = "El correo electrónico es requerido.")]
    [EmailAddress(ErrorMessage = "Debe ser un correo válido.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida.")]
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación de contraseña deben coincidir.")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "El balance inicial es requerido.")]
    public decimal InitialAmount { get; set; }
}

using System.ComponentModel.DataAnnotations;
namespace ArtemisBankingPro.Application.DTOs.Account;

public class ConfirmAccountRequest
{
    [Required(ErrorMessage = "El token es obligatorio.")]
    public string Token { get; set; } = string.Empty;
}
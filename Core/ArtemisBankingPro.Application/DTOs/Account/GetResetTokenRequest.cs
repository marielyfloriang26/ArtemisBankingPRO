using System.ComponentModel.DataAnnotations;
namespace ArtemisBankingPro.Application.DTOs.Account;

    public class GetResetTokenRequest
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        public string UserName { get; set; } = string.Empty;
    }

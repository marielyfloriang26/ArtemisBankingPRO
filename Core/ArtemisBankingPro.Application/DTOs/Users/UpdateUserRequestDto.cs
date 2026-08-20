namespace ArtemisBankingPro.Application.DTOs.Users;

public class UpdateUserRequestDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Identification { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public decimal? AdditionalAmount { get; set; }
}
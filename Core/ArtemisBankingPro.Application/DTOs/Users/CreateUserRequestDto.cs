namespace ArtemisBankingPro.Application.DTOs.Users;

public class CreateUserRequestDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Identification { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
    public string Role { get; set; } = null!;
    public decimal? InitialAmount { get; set; }
}
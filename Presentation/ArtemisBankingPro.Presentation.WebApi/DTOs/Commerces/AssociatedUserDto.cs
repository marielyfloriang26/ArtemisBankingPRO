namespace ArtemisBankingPro.Presentation.WebApi.DTOs.Commerces;

public class AssociatedUserDto
{
    public string Id { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
}

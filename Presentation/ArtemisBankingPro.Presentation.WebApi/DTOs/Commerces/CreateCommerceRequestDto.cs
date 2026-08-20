namespace ArtemisBankingPro.Presentation.WebApi.DTOs.Commerces;

public class CreateCommerceRequestDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Rnc { get; set; }
}

namespace ArtemisBankingPro.Application.DTOs.Users;

public class ErrorResponseDto
{
    public string Message { get; set; } = null!;

    public ErrorResponseDto(string message)
    {
        Message = message;
    }
}
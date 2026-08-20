namespace ArtemisBankingPro.Presentation.WebApi.DTOs;

public class ErrorResponseDto
{
    public string Message { get; set; }

    public ErrorResponseDto(string message)
    {
        Message = message;
    }
}

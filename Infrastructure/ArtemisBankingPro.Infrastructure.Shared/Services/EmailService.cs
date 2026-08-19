using ArtemisBankingPro.Application.Interfaces.Services;

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Shared.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        // Mock email sending logic
        _logger.LogInformation("Sending email to {To} with subject '{Subject}'", to, subject);
        return Task.CompletedTask;
    }
}

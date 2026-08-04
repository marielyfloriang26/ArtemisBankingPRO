using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

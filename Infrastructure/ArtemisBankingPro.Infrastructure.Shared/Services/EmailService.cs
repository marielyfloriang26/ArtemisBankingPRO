using ArtemisBankingPro.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Shared.Services
{
    public class EmailService : IEmailService
    {
        public Task SendEmailAsync(string to, string subject, string body)
        {
            // Aquí iría la lógica real (SMTP, SendGrid, etc). 
            // Por ahora solo completp la tarea para que no rompa la aplicación
            return Task.CompletedTask; 
        }
    }
}
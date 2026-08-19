using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Infrastructure.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.Infrastructure.Shared;

public static class ServiceRegistration
{
    public static void AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Register shared infrastructure services (e.g. EmailService, UploadService)

        services.AddHostedService<DailyQuotaCheckService>();

        services.AddTransient<IEmailService, EmailService>();
    }
}

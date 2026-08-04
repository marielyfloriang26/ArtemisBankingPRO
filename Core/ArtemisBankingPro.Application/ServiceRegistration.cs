using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ArtemisBankingPro.Application;

public static class ServiceRegistration
{
    public static void AddApplicationLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        #region Services
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.IPrestamoService, ArtemisBankingPro.Application.Services.PrestamoService>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.ICreditCardService, ArtemisBankingPro.Application.Services.CreditCardService>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.IBeneficiarioService, ArtemisBankingPro.Application.Services.BeneficiarioService>();
        #endregion
    }
}

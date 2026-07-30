using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ArtemisBankingPro.Application;

public static class ServiceRegistration
{
    public static void AddApplicationLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        #region Services
        // TODO: Register application services here
        #endregion
    }
}

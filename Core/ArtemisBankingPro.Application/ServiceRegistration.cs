using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ArtemisBankingPro.Application;

public static class ServiceRegistration
{
    public static void AddApplicationLayer(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ArtemisBankingPro.Application.Behaviors.ValidationBehavior<,>));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        #region Services

        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.IPrestamoService, ArtemisBankingPro.Application.Services.PrestamoService>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.ICreditCardService, ArtemisBankingPro.Application.Services.CreditCardService>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.IBeneficiarioService, ArtemisBankingPro.Application.Services.BeneficiarioService>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.ITransaccionClienteService, ArtemisBankingPro.Application.Services.TransaccionClienteService>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.ITransaccionCajeroService, ArtemisBankingPro.Application.Services.TransaccionCajeroService>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.IComercioService, ArtemisBankingPro.Application.Services.ComercioService>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.ITarjetaCreditoService, ArtemisBankingPro.Application.Services.TarjetaCreditoService>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.ICuentaAhorroService, ArtemisBankingPro.Application.Services.CuentaAhorroService>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Services.IHermesPayService, ArtemisBankingPro.Application.Services.HermesPayService>();
        #endregion
    }
}

using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArtemisBankingPro.Infrastructure.Persistence;

public static class ServiceRegistration
{
    public static void AddPersistenceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                m => m.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        #region Repositories
        services.AddTransient(typeof(ArtemisBankingPro.Application.Interfaces.Repositories.IGenericRepository<>), typeof(ArtemisBankingPro.Infrastructure.Persistence.Repositories.GenericRepository<>));
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Repositories.IPrestamoRepository, ArtemisBankingPro.Infrastructure.Persistence.Repositories.PrestamoRepository>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Repositories.ICuotaPrestamoRepository, ArtemisBankingPro.Infrastructure.Persistence.Repositories.CuotaPrestamoRepository>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Repositories.ICuentaAhorroRepository, ArtemisBankingPro.Infrastructure.Persistence.Repositories.CuentaAhorroRepository>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Repositories.ITransaccionRepository, ArtemisBankingPro.Infrastructure.Persistence.Repositories.TransaccionRepository>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Repositories.IUsuarioRepository, ArtemisBankingPro.Infrastructure.Persistence.Repositories.UsuarioRepository>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Repositories.IProductoTarjetaCreditoRepository, ArtemisBankingPro.Infrastructure.Persistence.Repositories.ProductoTarjetaCreditoRepository>();
        services.AddTransient<ArtemisBankingPro.Application.Interfaces.Repositories.IBeneficiarioRepository, ArtemisBankingPro.Infrastructure.Persistence.Repositories.BeneficiarioRepository>();
        #endregion
    }
}

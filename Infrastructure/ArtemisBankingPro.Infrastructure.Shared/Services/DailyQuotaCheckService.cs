using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Shared.Services;

public class DailyQuotaCheckService : BackgroundService
{
    private readonly ILogger<DailyQuotaCheckService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DailyQuotaCheckService(ILogger<DailyQuotaCheckService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily Quota Check Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndMarkLateQuotasAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing daily quota check.");
            }

            // Run once a day. For testing, it could be shorter, but we'll set it to 24 hours.
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CheckAndMarkLateQuotasAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var prestamoRepo = scope.ServiceProvider.GetRequiredService<IPrestamoRepository>();

        var prestamosActivos = await prestamoRepo.GetAllWithIncludesAsync();
        prestamosActivos = prestamosActivos.Where(p => p.Estado == "Activo").ToList();

        int lateQuotasCount = 0;
        var today = DateTime.Now.Date;

        foreach (var prestamo in prestamosActivos)
        {
            if (prestamo.Cuotas == null) continue;

            bool hasChanges = false;
            foreach (var cuota in prestamo.Cuotas)
            {
                if (cuota.EstadoPago != "Pagada" && cuota.FechaVencimiento.Date < today && !cuota.TieneAtraso)
                {
                    cuota.TieneAtraso = true;
                    hasChanges = true;
                    lateQuotasCount++;
                }
            }

            if (hasChanges)
            {
                await prestamoRepo.UpdateAsync(prestamo, prestamo.Id);
            }
        }

        _logger.LogInformation("Daily Quota Check Service completed. {Count} quotas marked as late.", lateQuotasCount);
    }
}

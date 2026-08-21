using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.BackgroundServices
{
    public class CuotasAtrasadasBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CuotasAtrasadasBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckCuotasAtrasadasAsync();
                // Run daily
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private async Task CheckCuotasAtrasadasAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var cuotaRepo = scope.ServiceProvider.GetRequiredService<ICuotaPrestamoRepository>();

            var cuotas = await cuotaRepo.GetAllAsync();
            var cuotasVencidas = cuotas.Where(c => c.EstadoPago != "Pagada" && c.FechaVencimiento < DateTime.Now.Date && !c.TieneAtraso).ToList();

            foreach (var cuota in cuotasVencidas)
            {
                cuota.TieneAtraso = true;
                await cuotaRepo.UpdateAsync(cuota, cuota.Id);
            }
        }
    }
}

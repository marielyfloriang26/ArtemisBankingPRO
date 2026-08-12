using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Cliente;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Application.Interfaces.Repositories;

namespace ArtemisBankingPro.Application.Services
{
    public class TarjetaCreditoService : ITarjetaCreditoService
    {
        private readonly IGenericRepository<TarjetaCredito> _tarjetaRepository;
        private readonly IGenericRepository<CuentaAhorro> _cuentaRepository;
        private readonly IGenericRepository<ConsumoTarjeta> _consumoRepository;
        private readonly IGenericRepository<Transaccion> _transaccionRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<TarjetaCreditoService> _logger;

        public TarjetaCreditoService(
            IGenericRepository<TarjetaCredito> tarjetaRepository,
            IGenericRepository<CuentaAhorro> cuentaRepository,
            IGenericRepository<ConsumoTarjeta> consumoRepository,
            IGenericRepository<Transaccion> transaccionRepository,
            IEmailService emailService,
            IMapper mapper,
            ILogger<TarjetaCreditoService> logger)
        {
            _tarjetaRepository = tarjetaRepository;
            _cuentaRepository = cuentaRepository;
            _consumoRepository = consumoRepository;
            _transaccionRepository = transaccionRepository;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<TarjetaCreditoViewModel>> GetActiveCardsByClientIdAsync(int clienteId)
        {
            var tarjetas = await _tarjetaRepository.GetAllAsync();
            var tarjetasActivas = tarjetas.Where(t => t.ClienteId == clienteId && t.Estado == "Activa").ToList();
            
            return _mapper.Map<List<TarjetaCreditoViewModel>>(tarjetasActivas);
        }

        public async Task<(bool Success, string ErrorMessage)> RealizarAvanceEfectivoAsync(AvanceEfectivoViewModel model, int clienteId)
        {
            // 1. Obtener la Tarjeta de Crédito
            var tarjetas = await _tarjetaRepository.GetAllAsync();
            var tarjeta = tarjetas.FirstOrDefault(t => t.Id == model.TarjetaCreditoId && t.ClienteId == clienteId && t.Estado == "Activa");

            if (tarjeta == null)
            {
                return (false, "La tarjeta de crédito seleccionada no es válida o no está activa.");
            }

            // 2. Obtener la Cuenta de Ahorro destino
            var cuentas = await _cuentaRepository.GetAllAsync();
            var cuenta = cuentas.FirstOrDefault(c => c.Id == model.CuentaAhorroDestinoId && c.ClienteId == clienteId && c.Estado == "Activa");

            if (cuenta == null)
            {
                return (false, "La cuenta de ahorro seleccionada no es válida o no está activa.");
            }

            // 3. Validar límite disponible
            decimal montoDisponible = tarjeta.LimiteCredito - tarjeta.MontoAdeudado;
            if (model.Monto > montoDisponible)
            {
                return (false, "El monto del avance supera el límite de crédito disponible en la tarjeta.");
            }

            // 4. Calcular interés del 6.25%
            decimal porcentajeInteres = 6.25m / 100m;
            decimal montoInteres = model.Monto * porcentajeInteres;
            decimal montoTotalAdeudar = model.Monto + montoInteres;

            // 5. Actualizar balances
            tarjeta.MontoAdeudado += montoTotalAdeudar;
            cuenta.Balance += model.Monto;

            // 6. Registrar Consumo en la Tarjeta
            var consumo = new ConsumoTarjeta
            {
                TarjetaId = tarjeta.Id,
                Monto = montoTotalAdeudar,
                Comercio = "AVANCE",
                Estado = "APROBADO",
                FechaConsumo = DateTime.UtcNow
            };

            // 7. Registrar Transacción
            var transaccion = new Transaccion
            {
                CuentaDestinoId = cuenta.Id,
                Monto = model.Monto,
                TipoTransaccion = "CRÉDITO",
                Origen = "AVANCE",
                Beneficiario = $"nº tarjeta {EnmascararTarjeta(tarjeta.NumeroTarjeta)}",
                Estado = "APROBADA",
                UsuarioResponsableId = clienteId,
                FechaTransaccion = DateTime.UtcNow
            };

            try
            {
                // Guardar cambios usando los repositorios
                await _tarjetaRepository.UpdateAsync(tarjeta, tarjeta.Id);
                await _cuentaRepository.UpdateAsync(cuenta, cuenta.Id);
                await _consumoRepository.AddAsync(consumo);
                await _transaccionRepository.AddAsync(transaccion);

                // Loggear en Serilog sin exponer el número completo
                string tarjetaOculta = EnmascararTarjeta(tarjeta.NumeroTarjeta);
                _logger.LogInformation("Avance de efectivo exitoso. Cliente: {ClienteId}, Tarjeta: {Tarjeta}, Cuenta Destino: {CuentaId}, Monto Solicitado: {Monto}, Interés: {Interes}", 
                    clienteId, tarjetaOculta, cuenta.Id, model.Monto, montoInteres);

                // (Opcional) Enviar correo
                // await _emailService.SendEmailAsync("correo@cliente.com", "Avance de Efectivo Realizado", $"Se ha realizado un avance de efectivo por {model.Monto} a su cuenta.");

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar el avance de efectivo para el cliente {ClienteId}", clienteId);
                return (false, "Ocurrió un error inesperado al procesar la transacción.");
            }
        }

        private string EnmascararTarjeta(string numeroTarjeta)
        {
            if (string.IsNullOrWhiteSpace(numeroTarjeta) || numeroTarjeta.Length < 4)
                return "****";
            
            return new string('*', numeroTarjeta.Length - 4) + numeroTarjeta.Substring(numeroTarjeta.Length - 4);
        }

        public async Task<(bool Success, string ErrorMessage)> RealizarPagoAsync(ViewModels.Cajero.PagoTarjetaViewModel model, int cajeroId)
{
    var tarjetas = await _tarjetaRepository.GetAllAsync();
    var tarjeta = tarjetas.FirstOrDefault(t => t.NumeroTarjeta == model.NumeroTarjeta && t.Estado == "Activa");

    if (tarjeta == null) return (false, "La tarjeta no existe o no está activa.");

    tarjeta.MontoAdeudado -= model.Monto;

    var consumo = new ConsumoTarjeta
    {
        TarjetaId = tarjeta.Id,
        Monto = model.Monto,
        Comercio = "PAGO_CAJA",
        Estado = "APROBADO",
        FechaConsumo = DateTime.UtcNow
    };

    var transaccion = new Transaccion
    {
        Monto = model.Monto,
        TipoTransaccion = "CRÉDITO",
        Origen = "PAGO",
        Beneficiario = $"nº tarjeta {EnmascararTarjeta(tarjeta.NumeroTarjeta)}",
        Estado = "APROBADA",
        UsuarioResponsableId = cajeroId,
        FechaTransaccion = DateTime.UtcNow
    };

    try
    {
        await _tarjetaRepository.UpdateAsync(tarjeta, tarjeta.Id);
        await _consumoRepository.AddAsync(consumo);
        await _transaccionRepository.AddAsync(transaccion);
        return (true, string.Empty);
    }
    catch
    {
        return (false, "Error al procesar el pago.");
    }
}
    }
}
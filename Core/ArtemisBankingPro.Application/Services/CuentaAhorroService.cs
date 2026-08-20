using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Cliente;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ArtemisBankingPro.Application.Services
{
    public class CuentaAhorroService : ICuentaAhorroService
    {
        private readonly IGenericRepository<CuentaAhorro> _cuentaRepository;
        private readonly IGenericRepository<Transaccion> _transaccionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CuentaAhorroService> _logger;
        public CuentaAhorroService(
            IGenericRepository<CuentaAhorro> cuentaRepository,
            IGenericRepository<Transaccion> transaccionRepository,
            IMapper mapper,
            ILogger<CuentaAhorroService> logger)
        {
            _cuentaRepository = cuentaRepository;
            _transaccionRepository = transaccionRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<CuentaAhorroViewModel>> GetActiveCuentasByClientIdAsync(int clienteId)
        {
            var cuentas = await _cuentaRepository.GetAllAsync();
            var cuentasActivas = cuentas.Where(c => c.ClienteId == clienteId && c.Estado == "Activa").ToList();
            
            return _mapper.Map<List<CuentaAhorroViewModel>>(cuentasActivas);
        }

        public async Task<(bool Success, string ErrorMessage)> RealizarTransferenciaAsync(TransferenciaViewModel model, int clienteId)
        {
            var cuentas = await _cuentaRepository.GetAllAsync();
            
            // 1. Obtener y validar cuenta origen
            var cuentaOrigen = cuentas.FirstOrDefault(c => c.Id == model.CuentaOrigenId && c.ClienteId == clienteId && c.Estado == "Activa");
            if (cuentaOrigen == null) 
                return (false, "La cuenta de origen no es válida.");
            if (cuentaOrigen.Balance < model.Monto) 
                return (false, "Balance insuficiente en la cuenta de origen.");
            // 2. Obtener y validar cuenta destino por su número
            var cuentaDestino = cuentas.FirstOrDefault(c => c.NumeroCuenta == model.NumeroCuentaDestino && c.Estado == "Activa");
            if (cuentaDestino == null) 
                return (false, "La cuenta de destino no existe o no está activa.");
                
            if (cuentaOrigen.Id == cuentaDestino.Id) 
                return (false, "No puede transferir a su misma cuenta de origen.");
            // 3. Actualizar balances
            cuentaOrigen.Balance -= model.Monto;
            cuentaDestino.Balance += model.Monto;
            // 4. Crear Transacción Débito (Origen)
            var transaccionOrigen = new Transaccion
            {
                CuentaOrigenId = cuentaOrigen.Id,
                CuentaDestinoId = cuentaDestino.Id,
                Monto = model.Monto,
                TipoTransaccion = "DÉBITO",
                Origen = "TRANSFERENCIA",
                Beneficiario = $"nº cuenta destino {cuentaDestino.NumeroCuenta}",
                Estado = "APROBADA",
                UsuarioResponsableId = clienteId,
                FechaTransaccion = DateTime.UtcNow
            };
            // 5. Crear Transacción Crédito (Destino)
            var transaccionDestino = new Transaccion
            {
                CuentaOrigenId = cuentaOrigen.Id,
                CuentaDestinoId = cuentaDestino.Id,
                Monto = model.Monto,
                TipoTransaccion = "CRÉDITO",
                Origen = $"nº cuenta origen {cuentaOrigen.NumeroCuenta}",
                Beneficiario = "TRANSFERENCIA",
                Estado = "APROBADA",
                UsuarioResponsableId = clienteId,
                FechaTransaccion = DateTime.UtcNow
            };
            try
            {
                await _cuentaRepository.UpdateAsync(cuentaOrigen, cuentaOrigen.Id);
                await _cuentaRepository.UpdateAsync(cuentaDestino, cuentaDestino.Id);
                await _transaccionRepository.AddAsync(transaccionOrigen);
                await _transaccionRepository.AddAsync(transaccionDestino);
                _logger.LogInformation("Transferencia exitosa. Origen: {Origen}, Destino: {Destino}, Monto: {Monto}", cuentaOrigen.NumeroCuenta, cuentaDestino.NumeroCuenta, model.Monto);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en transferencia desde la cuenta {Origen}", cuentaOrigen.NumeroCuenta);
                return (false, "Error al procesar la transferencia.");
            }
        }

                public async Task<(bool Success, string ErrorMessage)> RealizarDepositoAsync(ViewModels.Cajero.DepositoViewModel model, int cajeroId)
        {
            var cuentas = await _cuentaRepository.GetAllAsync();
            var cuenta = cuentas.FirstOrDefault(c => c.NumeroCuenta == model.NumeroCuenta && c.Estado == "Activa");

            if (cuenta == null)
                return (false, "La cuenta especificada no existe o no está activa.");

            // Aumentar balance
            cuenta.Balance += model.Monto;

            // Registrar Transacción (Crédito a la cuenta)
            var transaccion = new Transaccion
            {
                CuentaDestinoId = cuenta.Id,
                Monto = model.Monto,
                TipoTransaccion = "CRÉDITO",
                Origen = "DEPÓSITO",
                Beneficiario = "DEPÓSITO",
                Estado = "APROBADA",
                UsuarioResponsableId = cajeroId, // ID del Cajero que hace la operación
                FechaTransaccion = DateTime.UtcNow
            };

            try
            {
                await _cuentaRepository.UpdateAsync(cuenta, cuenta.Id);
                await _transaccionRepository.AddAsync(transaccion);
                
                _logger.LogInformation("Depósito exitoso. Cuenta: {Cuenta}, Monto: {Monto}, Cajero: {Cajero}", cuenta.NumeroCuenta, model.Monto, cajeroId);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar depósito a la cuenta {Cuenta}", cuenta.NumeroCuenta);
                return (false, "Error al procesar el depósito.");
            }
        }

        public async Task<(bool Success, string ErrorMessage)> RealizarRetiroAsync(ViewModels.Cajero.RetiroViewModel model, int cajeroId)
{
    var cuentas = await _cuentaRepository.GetAllAsync();
    var cuenta = cuentas.FirstOrDefault(c => c.NumeroCuenta == model.NumeroCuenta && c.Estado == "Activa");

    if (cuenta == null) return (false, "La cuenta no existe o no está activa.");
    if (cuenta.Balance < model.Monto) return (false, "La cuenta no tiene balance suficiente para el retiro.");

    cuenta.Balance -= model.Monto;

    var transaccion = new Transaccion
    {
        CuentaOrigenId = cuenta.Id,
        Monto = model.Monto,
        TipoTransaccion = "DÉBITO",
        Origen = "RETIRO",
        Beneficiario = "RETIRO",
        Estado = "APROBADA",
        UsuarioResponsableId = cajeroId,
        FechaTransaccion = DateTime.UtcNow
    };

    try
    {
        await _cuentaRepository.UpdateAsync(cuenta, cuenta.Id);
        await _transaccionRepository.AddAsync(transaccion);
        return (true, string.Empty);
    }
    catch
    {
        return (false, "Error al procesar el retiro.");
    }
}


    public async Task<List<TransaccionDetalleViewModel>> GetTransaccionesByCuentaIdAsync(int cuentaId, int clienteId)
{
    var cuenta = await _cuentaRepository.GetByIdAsync(cuentaId);
    if (cuenta == null || cuenta.ClienteId != clienteId)
    {
        return null;
    }

    var transacciones = await _transaccionRepository.GetAllAsync();
    
    var transaccionesCuenta = transacciones
        .Where(t => t.CuentaOrigenId == cuentaId || t.CuentaDestinoId == cuentaId)
        .OrderByDescending(t => t.FechaTransaccion)
        .Select(t => new TransaccionDetalleViewModel
        {
            FechaTransaccion = DateTime.Now,
            Monto = t.Monto,
            TipoTransaccion = t.TipoTransaccion,
            Beneficiario = t.Beneficiario ?? "N/D",
            Origen = t.Origen ?? "N/D",
            Estado = t.Estado ?? "APROBADA"
        })
        .ToList();

    return transaccionesCuenta;
}

    }
}
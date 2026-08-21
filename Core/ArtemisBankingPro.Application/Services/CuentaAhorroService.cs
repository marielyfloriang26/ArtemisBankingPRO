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
        private readonly IGenericRepository<Prestamo> _prestamoRepository;
        private readonly IMapper _mapper;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IEmailService _emailService;
        private readonly ILogger<CuentaAhorroService> _logger;
        public CuentaAhorroService(
            IGenericRepository<CuentaAhorro> cuentaRepository,
            IGenericRepository<Transaccion> transaccionRepository,
            IGenericRepository<Prestamo> prestamoRepository, 
            IMapper mapper,
            ILogger<CuentaAhorroService> logger, IUsuarioRepository usuarioRepo,
            IEmailService emailService)
        {
            _cuentaRepository = cuentaRepository;
            _transaccionRepository = transaccionRepository;
            _prestamoRepository = prestamoRepository;
            _mapper = mapper;
            _logger = logger;
            _usuarioRepo = usuarioRepo;
            _emailService = emailService;
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
            
            // Obtener y validar cuenta origen
            var cuentaOrigen = cuentas.FirstOrDefault(c => c.Id == model.CuentaOrigenId && c.ClienteId == clienteId && c.Estado == "Activa");
            if (cuentaOrigen == null) 
                return (false, "La cuenta de origen no es válida.");

            var cuentaDestino = cuentas.FirstOrDefault(c => c.NumeroCuenta == model.NumeroCuentaDestino && c.Estado == "Activa");

            if (cuentaOrigen.Balance < model.Monto)
            {
                await _transaccionRepository.AddAsync(new Transaccion
                {
                    CuentaOrigenId = cuentaOrigen.Id,
                    Monto = model.Monto,
                    TipoTransaccion = "DÉBITO",
                    Origen = "TRANSFERENCIA",
                    Beneficiario = model.NumeroCuentaDestino,
                    Estado = "RECHAZADA",
                    UsuarioResponsableId = clienteId,
                    FechaTransaccion = DateTime.UtcNow
                });
                return (false, "Balance insuficiente en la cuenta de origen.");
            }

            if (cuentaDestino == null)
            {
                await _transaccionRepository.AddAsync(new Transaccion
                {
                    CuentaOrigenId = cuentaOrigen.Id,
                    Monto = model.Monto,
                    TipoTransaccion = "DÉBITO",
                    Origen = "TRANSFERENCIA",
                    Beneficiario = model.NumeroCuentaDestino,
                    Estado = "RECHAZADA",
                    UsuarioResponsableId = clienteId,
                    FechaTransaccion = DateTime.UtcNow
                });
                return (false, "La cuenta de destino no existe o no está activa.");
            }

            if (cuentaOrigen.Id == cuentaDestino.Id)
            {
                await _transaccionRepository.AddAsync(new Transaccion
                {
                    CuentaOrigenId = cuentaOrigen.Id,
                    Monto = model.Monto,
                    TipoTransaccion = "DÉBITO",
                    Origen = "TRANSFERENCIA",
                    Beneficiario = model.NumeroCuentaDestino,
                    Estado = "RECHAZADA",
                    UsuarioResponsableId = clienteId,
                    FechaTransaccion = DateTime.UtcNow
                });
                return (false, "No puede transferir a su misma cuenta de origen.");
            }
           
           // Actualizar balances
            cuentaOrigen.Balance -= model.Monto;
            cuentaDestino.Balance += model.Monto;
            // Crear transaccion debito (Origen)
            var transaccionOrigen = new Transaccion
            {
                CuentaOrigenId = cuentaOrigen.Id,
                Monto = model.Monto,
                TipoTransaccion = "DÉBITO",
                Origen = "TRANSFERENCIA",
                Beneficiario = $"nº cuenta destino {cuentaDestino.NumeroCuenta}",
                Estado = "APROBADA",
                UsuarioResponsableId = clienteId,
                FechaTransaccion = DateTime.Now
            };
            // Crear transaccion credito (Destino)
            var transaccionDestino = new Transaccion
            {
                CuentaDestinoId = cuentaDestino.Id,
                Monto = model.Monto,
                TipoTransaccion = "CRÉDITO",
                Origen = $"nº cuenta origen {cuentaOrigen.NumeroCuenta}",
                Beneficiario = "TRANSFERENCIA",
                Estado = "APROBADA",
                UsuarioResponsableId = clienteId,
                FechaTransaccion = DateTime.Now
            };
            try
            {
                await _cuentaRepository.UpdateAsync(cuentaOrigen, cuentaOrigen.Id);
                await _cuentaRepository.UpdateAsync(cuentaDestino, cuentaDestino.Id);
                await _transaccionRepository.AddAsync(transaccionOrigen);
                await _transaccionRepository.AddAsync(transaccionDestino);
                _logger.LogInformation("Transferencia exitosa. Origen: {Origen}, Destino: {Destino}, Monto: {Monto}", cuentaOrigen.NumeroCuenta, cuentaDestino.NumeroCuenta, model.Monto);

                // Enviar correo de notificacion de la transferencia
                var cliente = await _usuarioRepo.GetByIdAsync(clienteId);
                string ultimos4Origen = cuentaOrigen.NumeroCuenta.Substring(cuentaOrigen.NumeroCuenta.Length - 4);
                string ultimos4Destino = cuentaDestino.NumeroCuenta.Substring(cuentaDestino.NumeroCuenta.Length - 4);
                var fecha = DateTime.Now;

                string asunto = $"Transferencia realizada a la cuenta {ultimos4Destino}";
                string cuerpo = $"Hola {cliente!.Nombre},\n\n" +
                $"Se ha realizado una transferencia desde su cuenta terminada en {ultimos4Origen} hacia la cuenta terminada en {ultimos4Destino}.\n" +
                $"Monto transferido: RD${model.Monto}\n" +
                $"Fecha y hora: {fecha}\n\n" +
                $"Si usted no reconoce esta operación, comuníquese con la entidad bancaria.";

                try
                {
                    await _emailService.SendEmailAsync(cliente.Email!, asunto, cuerpo);
                }
                catch
                {
                    return (true, "La transferencia fue realizada correctamente, pero no fue posible enviar el correo de notificación.");
                }

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

            // Registrar transaccion (credito a la cuenta)
            var transaccion = new Transaccion
            {
                CuentaDestinoId = cuenta.Id,
                Monto = model.Monto,
                TipoTransaccion = "CRÉDITO",
                Origen = "DEPÓSITO",
                Beneficiario = "DEPÓSITO",
                Estado = "APROBADA",
                UsuarioResponsableId = cajeroId, // ID del Cajero que hace la operacion
                FechaTransaccion = DateTime.Now
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
        FechaTransaccion = DateTime.Now
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
// 
    public async Task<(List<(CuentaAhorro Cuenta, Usuario? Cliente)> Cuentas, int TotalRegistros)> GetAllPaginatedAsync(
    int page, int pageSize, string? identification, string status, string type)
{
    var todasLasCuentas = await _cuentaRepository.GetAllAsync();
    var clientes = await _usuarioRepo.GetAllClientesAsync();

    var join = from c in todasLasCuentas
               join cl in clientes on c.ClienteId equals cl.Id into clienteGroup
               from cl in clienteGroup.DefaultIfEmpty()
               select new { Cuenta = c, Cliente = cl };

    if (type != "todas")
        join = join.Where(x => x.Cuenta.TipoCuenta.ToLower() == type.ToLower());

    if (!string.IsNullOrWhiteSpace(identification))
    {
        string cedulaLimpia = identification.Replace("-", "").Trim();
        join = join.Where(x => x.Cliente != null && x.Cliente.Cedula.Replace("-", "") == cedulaLimpia);

        if (status == "cancelada")
            join = join.Where(x => x.Cuenta.Estado == "Cancelada");
        else if (status == "activa")
            join = join.Where(x => x.Cuenta.Estado == "Activa");
        // si status == "todas", no filtra por estado

        join = join.OrderByDescending(x => x.Cuenta.Estado == "Activa")
                    .ThenByDescending(x => x.Cuenta.FechaCreacion);
    }
    else
    {
        if (status == "activa") join = join.Where(x => x.Cuenta.Estado == "Activa");
        else if (status == "cancelada") join = join.Where(x => x.Cuenta.Estado == "Cancelada");

        join = join.OrderByDescending(x => x.Cuenta.FechaCreacion);
    }

    var lista = join.Select(x => (x.Cuenta, x.Cliente)).ToList();

    int totalRegistros = lista.Count;
    var paginadas = lista.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    return (paginadas, totalRegistros);
}

public async Task<(bool Success, string ErrorMessage, CuentaAhorro? CuentaCreada, Usuario? Cliente)> CreateSecondaryAccountAsync(
    int clienteId, decimal balanceInicial, int adminId)
{
    if (balanceInicial < 0)
        return (false, "El balance inicial no puede ser negativo.", null, null);
    
    var cliente = await _usuarioRepo.GetByIdAsync(clienteId);
    if (cliente == null)
        return (false, "El cliente no existe.", null, null);
    if (!cliente.EsActivo)
        return (false, "El cliente está inactivo.", null, null);

    var todasLasCuentas = await _cuentaRepository.GetAllAsync();

    // Verificar que el cliente tenga cuenta principal activa
    var cuentaPrincipal = todasLasCuentas
        .FirstOrDefault(c => c.ClienteId == clienteId && c.TipoCuenta == "Principal" && c.Estado == "Activa");

    if (cuentaPrincipal == null)
        return (false, "El cliente no tiene una cuenta principal activa.", null, null);

    var todosLosPrestamos = await _prestamoRepository.GetAllAsync(); 

    // Generar num de cuenta unico de 9 dígitos
    var rng = new Random();
    string numeroCuenta;
    int intentos = 0;
    do
    {
        numeroCuenta = rng.Next(100000000, 999999999).ToString();
        intentos++;
        if (intentos > 20)
            return (false, "No fue posible generar un número de cuenta único.", null, null);
    }
    while (todasLasCuentas.Any(c => c.NumeroCuenta == numeroCuenta)|| todosLosPrestamos.Any(p => p.NumeroPrestamo == numeroCuenta));

    var nuevaCuenta = new CuentaAhorro
    {
        NumeroCuenta = numeroCuenta,
        ClienteId = clienteId,
        Balance = balanceInicial,
        TipoCuenta = "Secundaria",
        Estado = "Activa",
        FechaCreacion = DateTime.Now
    };

    await _cuentaRepository.AddAsync(nuevaCuenta);

    // Si tiene balance inicial, registrar transacción de credito
    if (balanceInicial > 0)
    {
        var transaccion = new Transaccion
        {
            CuentaDestinoId = nuevaCuenta.Id,
            Monto = balanceInicial,
            TipoTransaccion = "CRÉDITO",
            Origen = "ASIGNACIÓN INICIAL",
            Beneficiario = numeroCuenta,
            Estado = "APROBADA",
            UsuarioResponsableId = adminId,
            FechaTransaccion = DateTime.Now
        };
        await _transaccionRepository.AddAsync(transaccion);
    }

    return (true, string.Empty, nuevaCuenta, cliente);
}

public async Task<(CuentaAhorro? Cuenta, Usuario? Cliente, List<Transaccion> Transacciones, int TotalRegistros)> GetTransaccionesByAccountAsync(
    string numeroCuenta, int page, int pageSize)
{
    var todasLasCuentas = await _cuentaRepository.GetAllAsync();
    var cuenta = todasLasCuentas.FirstOrDefault(c => c.NumeroCuenta == numeroCuenta);

    if (cuenta == null)
        return (null, null, new List<Transaccion>(), 0);

    var cliente = await _usuarioRepo.GetByIdAsync(cuenta.ClienteId);

    var todasLasTransacciones = await _transaccionRepository.GetAllAsync();

    var transacciones = todasLasTransacciones
        .Where(t => t.CuentaOrigenId == cuenta.Id || t.CuentaDestinoId == cuenta.Id)
        .OrderByDescending(t => t.FechaTransaccion)
        .ToList();

    int totalRegistros = transacciones.Count;
    var paginadas = transacciones.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    return (cuenta, cliente, paginadas, totalRegistros);
}

public async Task<(bool Success, string ErrorMessage)> CancelSecondaryAccountAsync(
    string numeroCuenta, int adminId)
{
    var todasLasCuentas = await _cuentaRepository.GetAllAsync();
    var cuenta = todasLasCuentas.FirstOrDefault(c => c.NumeroCuenta == numeroCuenta);

    if (cuenta == null)
        return (false, "La cuenta indicada no existe.");
    if (cuenta.TipoCuenta == "Principal")
        return (false, "Las cuentas principales no pueden ser canceladas.");
    if (cuenta.Estado == "Cancelada")
        return (false, "La cuenta ya está cancelada.");

    // Si tiene balance, transferirlo a la cuenta principal
    if (cuenta.Balance > 0)
    {
        var cuentaPrincipal = todasLasCuentas
            .FirstOrDefault(c => c.ClienteId == cuenta.ClienteId && c.TipoCuenta == "Principal" && c.Estado == "Activa");

        if (cuentaPrincipal == null)
            return (false, "El cliente no tiene cuenta principal activa para recibir los fondos.");

        decimal montoTransferir = cuenta.Balance;

        // debito en la cuenta secundaria
        var transaccionDebito = new Transaccion
        {
            CuentaOrigenId = cuenta.Id,
            CuentaDestinoId = cuentaPrincipal.Id,
            Monto = montoTransferir,
            TipoTransaccion = "DÉBITO",
            Origen = "CANCELACIÓN DE CUENTA",
            Beneficiario = cuentaPrincipal.NumeroCuenta,
            Estado = "APROBADA",
            UsuarioResponsableId = adminId,
            FechaTransaccion = DateTime.Now
        };

        // Credito en la cuenta principal
        var transaccionCredito = new Transaccion
        {
            CuentaOrigenId = cuenta.Id,
            CuentaDestinoId = cuentaPrincipal.Id,
            Monto = montoTransferir,
            TipoTransaccion = "CRÉDITO",
            Origen = cuenta.NumeroCuenta,
            Beneficiario = "CANCELACIÓN DE CUENTA",
            Estado = "APROBADA",
            UsuarioResponsableId = adminId,
            FechaTransaccion = DateTime.Now
        };

        cuentaPrincipal.Balance += montoTransferir;
        cuenta.Balance = 0;

        await _cuentaRepository.UpdateAsync(cuentaPrincipal, cuentaPrincipal.Id);
        await _transaccionRepository.AddAsync(transaccionDebito);
        await _transaccionRepository.AddAsync(transaccionCredito);
    }

    cuenta.Estado = "Cancelada";
    await _cuentaRepository.UpdateAsync(cuenta, cuenta.Id);

    return (true, string.Empty);
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
            FechaTransaccion = t.FechaTransaccion,
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
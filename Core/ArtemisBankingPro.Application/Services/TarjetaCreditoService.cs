using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography; 
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Logging;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Cliente;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Identity;

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
        private readonly UserManager<Usuario> _userManager;

        public TarjetaCreditoService(
            IGenericRepository<TarjetaCredito> tarjetaRepository,
            IGenericRepository<CuentaAhorro> cuentaRepository,
            IGenericRepository<ConsumoTarjeta> consumoRepository,
            IGenericRepository<Transaccion> transaccionRepository,
            IEmailService emailService,
            IMapper mapper,
            ILogger<TarjetaCreditoService> logger,
            UserManager<Usuario> userManager)
        {
            _tarjetaRepository = tarjetaRepository;
            _cuentaRepository = cuentaRepository;
            _consumoRepository = consumoRepository;
            _transaccionRepository = transaccionRepository;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
            _userManager = userManager;
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

    // ==================== MÉTODOS PARA WEB API ====================

        public async Task<(bool Success, string Message, object? Data)> GetCreditCardsPagedAsync(int page, int pageSize, string status, string? identification)
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 20)
                return (false, "Parámetros de paginación inválidos.", null);

            status = status.ToLower();
            if (status != "activa" && status != "cancelada" && status != "todas")
                return (false, "Estado no permitido.", null);

            var todas = await _tarjetaRepository.GetAllAsync();
            IEnumerable<TarjetaCredito> query = todas;

            if (!string.IsNullOrEmpty(identification))
            {
                query = query.Where(t => t.Cliente != null && t.Cliente.Cedula == identification);
                if (status == "todas")
                {
                    query = query.OrderByDescending(t => t.Estado == "Activa").ThenByDescending(t => t.FechaCreacion);
                }
            }
            else
            {
                if (status == "activa") query = query.Where(t => t.Estado == "Activa");
                else if (status == "cancelada") query = query.Where(t => t.Estado == "Cancelada");

                query = query.OrderByDescending(t => t.FechaCreacion);
            }

            int totalRecords = query.Count();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var cards = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var listData = cards.Select(t => new {
                id = t.Id.ToString(),
                maskedCardNumber = $"************{t.NumeroTarjeta.Substring(t.NumeroTarjeta.Length - 4)}",
                lastFourDigits = t.NumeroTarjeta.Substring(t.NumeroTarjeta.Length - 4),
                clientId = t.ClienteId.ToString(),
                clientFullName = t.Cliente != null ? $"{t.Cliente.Nombre} {t.Cliente.Apellido}" : "",
                creditLimit = t.LimiteCredito,
                availableCredit = t.LimiteCredito - t.MontoAdeudado,
                currentDebt = t.MontoAdeudado,
                expirationDate = t.FechaExpiracion,
                status = t.Estado,
                createdAt = t.FechaCreacion.ToString("yyyy-MM-ddTHH:mm:ss")
            }).ToList();

            var response = new {
                page = page,
                pageSize = pageSize,
                totalRecords = totalRecords,
                totalPages = totalPages,
                data = listData
            };

            return (true, "OK", response);
        }

        public async Task<(bool Success, string Message, object? Data)> AssignCreditCardAsync(string clientIdStr, decimal creditLimit, int adminUserId)
        {
            if (creditLimit <= 0)
                return (false, "El límite debe ser mayor a cero.", null);

            if (!int.TryParse(clientIdStr, out int clientId))
                return (false, "Cliente no encontrado.", null);

            var cliente = await _userManager.FindByIdAsync(clientIdStr);
            if (cliente == null || !cliente.EsActivo)
                return (false, "El cliente no existe o está inactivo.", null);

            string numeroTarjeta = await GenerarNumeroTarjetaUnicoAsync();
            string rawCvc = new Random().Next(100, 1000).ToString();
            string hashedCvc = HashSHA256(rawCvc);
            string fechaExpiracion = DateTime.UtcNow.AddYears(3).ToString("MM/yy");

            var tarjeta = new TarjetaCredito
            {
                NumeroTarjeta = numeroTarjeta,
                ClienteId = clientId,
                LimiteCredito = creditLimit,
                MontoAdeudado = 0.00m,
                FechaExpiracion = fechaExpiracion,
                CVC = hashedCvc,
                Estado = "Activa",
                AdminId = adminUserId,
                FechaCreacion = DateTime.UtcNow
            };

            var guardada = await _tarjetaRepository.AddAsync(tarjeta);

            if (!string.IsNullOrEmpty(cliente.Email))
            {
                try
                {
                    string ultimos4 = numeroTarjeta.Substring(numeroTarjeta.Length - 4);
                    await _emailService.SendEmailAsync(cliente.Email, "Asignación de Tarjeta", $"Se te asignó la tarjeta ****{ultimos4} con un límite de RD${creditLimit:N2}");
                }
                catch { }
            }

            string last4 = guardada.NumeroTarjeta.Substring(guardada.NumeroTarjeta.Length - 4);
            var data = new {
                id = guardada.Id.ToString(),
                maskedCardNumber = $"************{last4}",
                lastFourDigits = last4,
                clientId = cliente.Id.ToString(),
                clientFullName = $"{cliente.Nombre} {cliente.Apellido}",
                creditLimit = guardada.LimiteCredito,
                availableCredit = guardada.LimiteCredito,
                currentDebt = 0.00m,
                expirationDate = guardada.FechaExpiracion,
                status = guardada.Estado,
                createdAt = guardada.FechaCreacion.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            return (true, "Creada", data);
        }

        public async Task<(bool Success, string Message, object? Data)> GetCreditCardDetailsAsync(string id)
        {
            if (!int.TryParse(id, out int cardId)) return (false, "Not Found", null);

            var t = await _tarjetaRepository.GetByIdAsync(cardId);
            if (t == null) return (false, "Not Found", null);

            var consumos = await _consumoRepository.GetAllAsync();
            var listConsumos = consumos
                .Where(c => c.TarjetaId == cardId)
                .OrderByDescending(c => c.FechaConsumo)
                .Select(c => new {
                    id = c.Id.ToString(),
                    date = c.FechaConsumo.ToString("yyyy-MM-ddTHH:mm:ss"),
                    amount = c.Monto,
                    commerceName = c.Comercio,
                    status = c.Estado
                }).ToList();

            string last4 = t.NumeroTarjeta.Substring(t.NumeroTarjeta.Length - 4);
            var data = new {
                id = t.Id.ToString(),
                maskedCardNumber = $"************{last4}",
                lastFourDigits = last4,
                clientId = t.ClienteId.ToString(),
                clientFullName = t.Cliente != null ? $"{t.Cliente.Nombre} {t.Cliente.Apellido}" : "",
                creditLimit = t.LimiteCredito,
                availableCredit = t.LimiteCredito - t.MontoAdeudado,
                currentDebt = t.MontoAdeudado,
                expirationDate = t.FechaExpiracion,
                status = t.Estado,
                consumptions = listConsumos
            };

            return (true, "OK", data);
        }

        public async Task<(bool Success, string Message)> UpdateCreditLimitAsync(string id, decimal newLimit)
        {
            if (newLimit <= 0) return (false, "El límite debe ser mayor a cero.");
            if (!int.TryParse(id, out int cardId)) return (false, "Not Found");

            var tarjeta = await _tarjetaRepository.GetByIdAsync(cardId);
            if (tarjeta == null) return (false, "Not Found");
            if (tarjeta.Estado != "Activa") return (false, "La tarjeta no está activa.");
            if (newLimit < tarjeta.MontoAdeudado) return (false, "El nuevo límite no puede ser menor a la deuda actual.");

            tarjeta.LimiteCredito = newLimit;
            await _tarjetaRepository.UpdateAsync(tarjeta, cardId);

            var cliente = await _userManager.FindByIdAsync(tarjeta.ClienteId.ToString());
            if (cliente != null && !string.IsNullOrEmpty(cliente.Email))
            {
                try {
                    string last4 = tarjeta.NumeroTarjeta.Substring(tarjeta.NumeroTarjeta.Length - 4);
                    await _emailService.SendEmailAsync(cliente.Email, "Límite Actualizado", $"El nuevo límite de tu tarjeta ****{last4} es RD${newLimit:N2}.");
                } catch {}
            }

            return (true, "Actualizado");
        }

        public async Task<(bool Success, string Message)> CancelCreditCardAsync(string id)
        {
            if (!int.TryParse(id, out int cardId)) return (false, "Not Found");

            var tarjeta = await _tarjetaRepository.GetByIdAsync(cardId);
            if (tarjeta == null) return (false, "Not Found");
            if (tarjeta.Estado != "Activa") return (false, "La tarjeta ya está cancelada o inactiva.");
            if (tarjeta.MontoAdeudado > 0) return (false, "Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente.");

            tarjeta.Estado = "Cancelada";
            await _tarjetaRepository.UpdateAsync(tarjeta, cardId);

            return (true, "Cancelada");
        }

        private async Task<string> GenerarNumeroTarjetaUnicoAsync()
        {
            var random = new Random();
            string numero;
            bool existe;

            do
            {
                var sb = new StringBuilder();
                for (int i = 0; i < 16; i++)
                {
                    sb.Append(random.Next(0, 10));
                }
                numero = sb.ToString();

                var todas = await _tarjetaRepository.GetAllAsync();
                existe = todas.Any(t => t.NumeroTarjeta == numero);

            } while (existe);

            return numero;
        }

        private string HashSHA256(string rawData)
        {
            using var sha256Hash = SHA256.Create();
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

    
    }
}    
    

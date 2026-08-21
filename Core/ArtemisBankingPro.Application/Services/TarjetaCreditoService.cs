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
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IPrestamoService _prestamoService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<TarjetaCreditoService> _logger;
        private readonly UserManager<Usuario> _userManager;

        public TarjetaCreditoService(
            IGenericRepository<TarjetaCredito> tarjetaRepository,
            IGenericRepository<CuentaAhorro> cuentaRepository,
            IGenericRepository<ConsumoTarjeta> consumoRepository,
            IGenericRepository<Transaccion> transaccionRepository,
            IUsuarioRepository usuarioRepo,
            IPrestamoService prestamoService,
            IEmailService emailService,
            IMapper mapper,
            ILogger<TarjetaCreditoService> logger,
            UserManager<Usuario> userManager)
        {
            _tarjetaRepository = tarjetaRepository;
            _cuentaRepository = cuentaRepository;
            _consumoRepository = consumoRepository;
            _transaccionRepository = transaccionRepository;
            _usuarioRepo = usuarioRepo;
            _prestamoService = prestamoService;
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
            // Obtener la Tarjeta de credito
            var tarjetas = await _tarjetaRepository.GetAllAsync();
            var tarjeta = tarjetas.FirstOrDefault(t => t.Id == model.TarjetaCreditoId && t.ClienteId == clienteId && t.Estado == "Activa");

            if (tarjeta == null)
            {
                return (false, "La tarjeta de crédito seleccionada no es válida o no está activa.");
            }

            // Obtener la Cuenta de Ahorro destino
            var cuentas = await _cuentaRepository.GetAllAsync();
            var cuenta = cuentas.FirstOrDefault(c => c.Id == model.CuentaAhorroDestinoId && c.ClienteId == clienteId && c.Estado == "Activa" && c.TipoCuenta == "Principal");

            if (cuenta == null)
            {
                return (false, "La cuenta de ahorro seleccionada no es válida o no está activa.");
            }

            // Validar limite disponible
            decimal montoDisponible = tarjeta.LimiteCredito - tarjeta.MontoAdeudado;
            if (model.Monto > montoDisponible)
            {
                var consumoRechazado = new ConsumoTarjeta
                {
                    TarjetaId = tarjeta.Id,
                    Monto = model.Monto,
                    Comercio = "AVANCE",
                    Estado = "RECHAZADO",
                    FechaConsumo = DateTime.UtcNow
                };
                await _consumoRepository.AddAsync(consumoRechazado);

                return (false, "El monto del avance supera el límite de crédito disponible en la tarjeta.");
            }

            // Calcular interes del 6.25%
            decimal porcentajeInteres = 6.25m / 100m;
            decimal montoInteres = model.Monto * porcentajeInteres;
            decimal montoTotalAdeudar = model.Monto + montoInteres;

            // Actualizar balances
            tarjeta.MontoAdeudado += montoTotalAdeudar;
            cuenta.Balance += model.Monto;

            // Registrar Consumo en la Tarjeta
            var consumo = new ConsumoTarjeta
            {
                TarjetaId = tarjeta.Id,
                Monto = montoTotalAdeudar,
                Comercio = "AVANCE",
                Estado = "APROBADO",
                FechaConsumo = DateTime.UtcNow
            };

            // Registrar transaccion
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

                // Enviar correo de notifi del avance de efectivo
                var cliente = await _usuarioRepo.GetByIdAsync(clienteId);
                string ultimos4Tarjeta = tarjeta.NumeroTarjeta.Substring(12);
                string ultimos4Cuenta = cuenta.NumeroCuenta.Substring(cuenta.NumeroCuenta.Length - 4);

                                string asunto = $"Avance de efectivo desde la tarjeta {ultimos4Tarjeta}";
                string cuerpo = $@"
                    <div style='font-family: Arial, sans-serif; font-size: 15px; color: #222;'>
                        <p>Hola {cliente!.Nombre},</p>
                        <p>Se ha realizado un avance de efectivo desde su tarjeta terminada en <strong>{ultimos4Tarjeta}</strong>.</p>
                        <table style='margin: 16px 0; border-collapse: collapse;'>
                            <tr><td style='padding: 4px 12px 4px 0; color: #555;'>Monto depositado:</td><td><strong>RD${model.Monto:N2}</strong></td></tr>
                            <tr><td style='padding: 4px 12px 4px 0; color: #555;'>Interés aplicado:</td><td><strong>RD${montoInteres:N2}</strong></td></tr>
                            <tr><td style='padding: 4px 12px 4px 0; color: #555;'>Total cargado a la tarjeta:</td><td><strong>RD${montoTotalAdeudar:N2}</strong></td></tr>
                            <tr><td style='padding: 4px 12px 4px 0; color: #555;'>Cuenta destino terminada en:</td><td><strong>{ultimos4Cuenta}</strong></td></tr>
                            <tr><td style='padding: 4px 12px 4px 0; color: #555;'>Fecha y hora:</td><td><strong>{DateTime.Now:dd/MM/yyyy hh:mm tt}</strong></td></tr>
                        </table>
                        <p style='color: #777; font-size: 13px;'>Si usted no reconoce esta operación, comuníquese con la entidad bancaria.</p>
                    </div>";

                try
                {
                    await _emailService.SendEmailAsync(cliente.Email!, asunto, cuerpo);
                }
                catch
                {
                    return (true, "El avance fue realizado correctamente, pero no fue posible enviar el correo de notificación.");
                }

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

//
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

    //  METODOS PARA WEB API 

        public async Task<(bool Success, string Message, object? Data)> GetCreditCardsPagedAsync(int page, int pageSize, string status, string? identification)
{
    if (page <= 0 || pageSize <= 0 || pageSize > 20)
        return (false, "Parámetros de paginación inválidos.", null);

    status = status.ToLower();
    if (status != "activa" && status != "cancelada" && status != "todas")
        return (false, "Estado no permitido.", null);

    var todasTarjetas = await _tarjetaRepository.GetAllAsync();
    var clientes = await _usuarioRepo.GetAllClientesAsync();

    var join = from t in todasTarjetas
               join c in clientes on t.ClienteId equals c.Id into clienteGroup
               from c in clienteGroup.DefaultIfEmpty()
               select new { Tarjeta = t, Cliente = c };

    if (!string.IsNullOrEmpty(identification))
    {
        join = join.Where(x => x.Cliente != null && x.Cliente.Cedula == identification);
    }

    if (status == "activa")
    {
        join = join.Where(x => x.Tarjeta.Estado == "Activa")
                    .OrderByDescending(x => x.Tarjeta.FechaCreacion);
    }
    else if (status == "cancelada")
    {
        join = join.Where(x => x.Tarjeta.Estado == "Cancelada")
                    .OrderByDescending(x => x.Tarjeta.FechaCreacion);
    }
    else // todas
    {
        join = join.OrderByDescending(x => x.Tarjeta.Estado == "Activa")
                    .ThenByDescending(x => x.Tarjeta.FechaCreacion);
    }

    var joinList = join.ToList();
    int totalRecords = joinList.Count;
    int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

    var pageItems = joinList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    var listData = pageItems.Select(x => new {
        id = x.Tarjeta.Id.ToString(),
        maskedCardNumber = $"************{x.Tarjeta.NumeroTarjeta.Substring(x.Tarjeta.NumeroTarjeta.Length - 4)}",
        lastFourDigits = x.Tarjeta.NumeroTarjeta.Substring(x.Tarjeta.NumeroTarjeta.Length - 4),
        clientId = x.Tarjeta.ClienteId.ToString(),
        clientFullName = x.Cliente != null ? $"{x.Cliente.Nombre} {x.Cliente.Apellido}" : "",
        creditLimit = x.Tarjeta.LimiteCredito,
        availableCredit = x.Tarjeta.LimiteCredito - x.Tarjeta.MontoAdeudado,
        currentDebt = x.Tarjeta.MontoAdeudado,
        expirationDate = x.Tarjeta.FechaExpiracion,
        status = x.Tarjeta.Estado,
        createdAt = x.Tarjeta.FechaCreacion.ToString("yyyy-MM-ddTHH:mm:ss")
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

            // ojo
            var cliente = await _userManager.FindByIdAsync(clientIdStr);
            if (cliente == null)
            return (false, "El cliente no existe.", null);

            if (!cliente.EsActivo)
            return (false, "El cliente está inactivo.", null);  

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

            var cliente = await _usuarioRepo.GetByIdAsync(t.ClienteId);

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
                clientFullName = cliente != null ? $"{cliente.Nombre} {cliente.Apellido}" : "",
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
        /*
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
        } */

        public async Task<List<ArtemisBankingPro.Application.ViewModels.AdminTarjeta.TarjetaAdminViewModel>> GetAllTarjetasFilteredAsync(string? cedula, string estadoFiltro)
        {
            var tarjetas = await _tarjetaRepository.GetAllAsync();
            var clientes = await _usuarioRepo.GetAllClientesAsync();
            
            var join = from t in tarjetas
                       join c in clientes on t.ClienteId equals c.Id
                       select new { t, c };

            if (!string.IsNullOrEmpty(cedula))
            {
                join = join.Where(x => x.c.Cedula == cedula);
            }

            if (!string.IsNullOrEmpty(estadoFiltro) && estadoFiltro != "Todas")
            {
                string estadoReal = estadoFiltro == "Activas" ? "Activa" : "Cancelada";
                join = join.Where(x => x.t.Estado == estadoReal);
            }

            var ordered = join
                .OrderBy(x => x.t.Estado == "Activa" ? 0 : 1)
                .ThenByDescending(x => x.t.FechaCreacion)
                .ToList();

            return ordered.Select(x => new ArtemisBankingPro.Application.ViewModels.AdminTarjeta.TarjetaAdminViewModel
            {
                Id = x.t.Id,
                NumeroTarjeta = EnmascararTarjeta(x.t.NumeroTarjeta),
                Cliente = $"{x.c.Nombre} {x.c.Apellido}",
                LimiteCredito = x.t.LimiteCredito,
                FechaExpiracion = x.t.FechaExpiracion,
                MontoAdeudado = x.t.MontoAdeudado,
                Estado = x.t.Estado == "Activa" ? "Activa" : "Cancelada",
                Cedula = x.c.Cedula
            }).ToList();
        }

        public async Task<List<ArtemisBankingPro.Application.ViewModels.Prestamos.ClienteElegibleViewModel>> GetClientesElegiblesAsync(string? cedula)
        {
            var clientes = await _usuarioRepo.GetAllClientesAsync();
            clientes = clientes.Where(c => c.EsActivo).ToList();

            if (!string.IsNullOrEmpty(cedula))
            {
                clientes = clientes.Where(c => c.Cedula.Contains(cedula)).ToList();
            }

            var results = new List<ArtemisBankingPro.Application.ViewModels.Prestamos.ClienteElegibleViewModel>();
            foreach (var c in clientes)
            {
                decimal deuda = await _prestamoService.CalcularDeudaTotalClienteAsync(c.Id);
                results.Add(new ArtemisBankingPro.Application.ViewModels.Prestamos.ClienteElegibleViewModel
                {
                    Id = c.Id,
                    Cedula = c.Cedula,
                    NombreCompleto = $"{c.Nombre} {c.Apellido}",
                    Correo = c.Email ?? "",
                    DeudaTotal = deuda
                });
            }
            return results;
        }

        public async Task<decimal> CalcularDeudaPromedioGlobalAsync()
        {
            return await _prestamoService.CalcularDeudaPromedioGlobalAsync();
        }

        private string GenerarNumeroTarjetaUnico(IEnumerable<TarjetaCredito> tarjetasExistentes)
        {
            var rng = new Random();
            while (true)
            {
                string num = "";
                for(int i=0; i<16; i++) num += rng.Next(0, 10).ToString();
                
                if (!tarjetasExistentes.Any(t => t.NumeroTarjeta == num)) return num;
            }
        }

        private string GenerarCvcUnico()
        {
            var rng = new Random();
            return rng.Next(100, 999).ToString();
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public async Task<string> AsignarTarjetaAsync(ArtemisBankingPro.Application.ViewModels.AdminTarjeta.AssignTarjetaViewModel model)
        {
            var cliente = await _usuarioRepo.GetByIdAsync(model.ClienteId);
            if (cliente == null) return "El cliente seleccionado no existe.";
            if (!cliente.EsActivo) return "Solo se puede asignar tarjetas de crédito a clientes activos.";
            if (model.LimiteCredito <= 0) return "El límite de crédito debe ser mayor que cero.";

            var tarjetas = await _tarjetaRepository.GetAllAsync();
            string numTarjeta = GenerarNumeroTarjetaUnico(tarjetas);
            string cvcTexto = GenerarCvcUnico();
            string cvcHash = ComputeSha256Hash(cvcTexto);

            var tarjeta = new TarjetaCredito
            {
                ClienteId = model.ClienteId,
                NumeroTarjeta = numTarjeta,
                LimiteCredito = model.LimiteCredito,
                MontoAdeudado = 0,
                FechaExpiracion = DateTime.UtcNow.AddYears(3).ToString("MM/yy"),
                CVC = cvcHash,
                AdminId = model.AdminId,
                Estado = "Activa",
                FechaCreacion = DateTime.UtcNow
            };

            await _tarjetaRepository.AddAsync(tarjeta);

            try
            {
                string ultimos4 = numTarjeta.Substring(12);
                string exp = tarjeta.FechaExpiracion;
                await _emailService.SendEmailAsync(cliente.Email!, "Nueva tarjeta de crédito asignada", $"Tarjeta terminada en: {ultimos4}, Límite aprobado: {model.LimiteCredito}, Fecha de expiración: {exp}");
            }
            catch
            {
                return "La tarjeta fue creada correctamente, pero no fue posible enviar el correo de notificación.";
            }

            return string.Empty;
        }

        public async Task<List<ArtemisBankingPro.Application.ViewModels.AdminTarjeta.ConsumoTarjetaViewModel>> GetConsumosTarjetaAsync(int tarjetaId)
        {
            var consumos = await _consumoRepository.GetAllAsync();
            return consumos.Where(c => c.TarjetaId == tarjetaId)
                           .OrderByDescending(c => c.FechaConsumo)
                           .Select(c => new ArtemisBankingPro.Application.ViewModels.AdminTarjeta.ConsumoTarjetaViewModel
                           {
                               FechaConsumo = c.FechaConsumo,
                               MontoConsumido = c.Monto,
                               Comercio = c.Comercio,
                               EstadoConsumo = c.Estado
                           }).ToList();
        }

        public async Task<ArtemisBankingPro.Application.ViewModels.AdminTarjeta.EditLimiteTarjetaViewModel?> GetEditLimiteViewModelAsync(int id)
        {
            var tarjeta = await _tarjetaRepository.GetByIdAsync(id);
            if (tarjeta == null) return null;
            return new ArtemisBankingPro.Application.ViewModels.AdminTarjeta.EditLimiteTarjetaViewModel
            {
                TarjetaId = tarjeta.Id,
                NuevoLimite = tarjeta.LimiteCredito
            };
        }

        public async Task<string> EditLimiteAsync(ArtemisBankingPro.Application.ViewModels.AdminTarjeta.EditLimiteTarjetaViewModel model)
        {
            var tarjeta = await _tarjetaRepository.GetByIdAsync(model.TarjetaId);
            if (tarjeta == null) return "La tarjeta seleccionada no existe.";
            if (tarjeta.Estado != "Activa") return "No se puede modificar una tarjeta cancelada.";
            if (model.NuevoLimite <= 0) return "El límite de la tarjeta debe ser mayor que cero.";
            if (model.NuevoLimite < tarjeta.MontoAdeudado) return "El límite de la tarjeta no puede ser inferior al monto adeudado actualmente.";

            tarjeta.LimiteCredito = model.NuevoLimite;
            await _tarjetaRepository.UpdateAsync(tarjeta, tarjeta.Id);

            try
            {
                var cliente = await _usuarioRepo.GetByIdAsync(tarjeta.ClienteId);
                string ultimos4 = tarjeta.NumeroTarjeta.Substring(12);
                await _emailService.SendEmailAsync(cliente!.Email!, "Modificación de límite de tarjeta", $"El límite de su tarjeta de crédito terminada en {ultimos4} ha sido actualizado. Nuevo límite aprobado: {model.NuevoLimite}");
            }
            catch
            {
                return "El límite fue actualizado correctamente, pero no fue posible enviar el correo de notificación.";
            }

            return string.Empty;
        }

        public async Task<string> CancelTarjetaAsync(int id)
        {
            var tarjeta = await _tarjetaRepository.GetByIdAsync(id);
            if (tarjeta == null) return "La tarjeta seleccionada no existe.";
            if (tarjeta.MontoAdeudado > 0) return "Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente.";

            tarjeta.Estado = "Cancelada";
            await _tarjetaRepository.UpdateAsync(tarjeta, tarjeta.Id);
            return string.Empty;
        }

        public async Task<bool> ExisteClienteConCedulaAsync(string cedula)
        {
            var clientes = await _usuarioRepo.GetAllClientesAsync();
            return clientes.Any(c => c.Cedula == cedula);
        }

        public async Task<List<ArtemisBankingPro.Application.ViewModels.AdminTarjeta.ConsumoTarjetaViewModel>> GetConsumosByTarjetaIdAsync(int tarjetaId, int clienteId)
        {
            // Validar que la tarjeta pertenezca al cliente
            var tarjetas = await _tarjetaRepository.GetAllAsync();
            var tarjetaValida = tarjetas.Any(t => t.Id == tarjetaId && t.ClienteId == clienteId);

            if (!tarjetaValida)
            {
                return new List<ArtemisBankingPro.Application.ViewModels.AdminTarjeta.ConsumoTarjetaViewModel>();
            }

            // Obtener los consumos y mapearlos al ViewModel fuerte
            var consumos = await _consumoRepository.GetAllAsync();
            
            return consumos
                .Where(c => c.TarjetaId == tarjetaId)
                .OrderByDescending(c => c.FechaConsumo)
                .Select(c => new ArtemisBankingPro.Application.ViewModels.AdminTarjeta.ConsumoTarjetaViewModel
                {
                    FechaConsumo = c.FechaConsumo,
                    MontoConsumido = c.Monto,
                    Comercio = c.Comercio,
                    EstadoConsumo = c.Estado
                })
                .ToList();
        }

    }
}    
    

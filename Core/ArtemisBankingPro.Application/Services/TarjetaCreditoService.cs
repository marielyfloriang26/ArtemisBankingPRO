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
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IPrestamoService _prestamoService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<TarjetaCreditoService> _logger;

        public TarjetaCreditoService(
            IGenericRepository<TarjetaCredito> tarjetaRepository,
            IGenericRepository<CuentaAhorro> cuentaRepository,
            IGenericRepository<ConsumoTarjeta> consumoRepository,
            IGenericRepository<Transaccion> transaccionRepository,
            IUsuarioRepository usuarioRepo,
            IPrestamoService prestamoService,
            IEmailService emailService,
            IMapper mapper,
            ILogger<TarjetaCreditoService> logger)
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
            var cuenta = cuentas.FirstOrDefault(c => c.Id == model.CuentaAhorroDestinoId && c.ClienteId == clienteId && c.Estado == "Activa" && c.TipoCuenta == "Principal");

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
    }
}
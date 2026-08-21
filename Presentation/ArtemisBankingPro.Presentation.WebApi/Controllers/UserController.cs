using ArtemisBankingPro.Application.DTOs;
using ArtemisBankingPro.Application.DTOs.Users;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Administrador")]
public class UserController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public UserController(UserManager<Usuario> userManager, ApplicationDbContext context, IEmailService emailService)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? role = null)
    {
        if (page < 1 || pageSize < 1 || pageSize > 20)
            return BadRequest(new ErrorResponseDto("Parámetros de paginación inválidos (pageSize máximo 20)."));

        if (!string.IsNullOrEmpty(role))
        {
            var lowerRole = role.ToLower();
            if (lowerRole != "administrador" && lowerRole != "cajero" && lowerRole != "cliente")
                return BadRequest(new ErrorResponseDto("El rol de filtro solo puede ser administrador, cajero o cliente."));
        }

        var query = _userManager.Users.Where(u => u.TipoUsuario != "Comercio");

        if (!string.IsNullOrEmpty(role))
        {
            query = query.Where(u => u.TipoUsuario.ToLower() == role.ToLower());
        }

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var users = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var data = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            data.Add(new
            {
                id = u.Id.ToString(),
                userName = u.UserName,
                identification = u.Cedula,
                firstName = u.Nombre,
                lastName = u.Apellido,
                email = u.Email,
                role = roles.FirstOrDefault() ?? u.TipoUsuario,
                isActive = u.EsActivo
            });
        }

        return Ok(new
        {
            page,
            pageSize,
            totalRecords,
            totalPages,
            data
        });
    }

    [HttpGet("commerce")]
    public async Task<IActionResult> GetCommerceUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 20)
            return BadRequest(new ErrorResponseDto("Parámetros de paginación inválidos (pageSize máximo 20)."));

        var query = _context.Users
            .Where(u => u.TipoUsuario == "Comercio");

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var users = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var data = new List<object>();
        foreach (var u in users)
        {
            var rel = await _context.Set<ComercioUsuario>().Include(c => c.Comercio).FirstOrDefaultAsync(c => c.UsuarioId == u.Id);
            var roles = await _userManager.GetRolesAsync(u);

            data.Add(new
            {
                id = u.Id.ToString(),
                userName = u.UserName,
                identification = u.Cedula,
                firstName = u.Nombre,
                lastName = u.Apellido,
                email = u.Email,
                role = roles.FirstOrDefault() ?? "Comercio",
                commerceId = rel?.ComercioId,
                commerceName = rel?.Comercio?.Nombre,
                isActive = u.EsActivo
            });
        }

        return Ok(new
        {
            page,
            pageSize,
            totalRecords,
            totalPages,
            data
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponseDto("Datos faltantes o inválidos."));

        if (request.Role != "Administrador" && request.Role != "Cajero" && request.Role != "Cliente")
            return BadRequest(new ErrorResponseDto("El rol debe ser Administrador, Cajero o Cliente."));

        if (request.Password != request.ConfirmPassword)
            return BadRequest(new ErrorResponseDto("Las contraseñas no coinciden."));

        if (await _userManager.FindByNameAsync(request.UserName) != null ||
            await _userManager.FindByEmailAsync(request.Email) != null ||
            await _context.Users.AnyAsync(u => u.Cedula == request.Identification))
        {
            return Conflict(new ErrorResponseDto("El usuario, correo o cédula ya se encuentra registrado."));
        }

        var user = new Usuario
        {
            Nombre = request.FirstName,
            Apellido = request.LastName,
            Cedula = request.Identification,
            Email = request.Email,
            UserName = request.UserName,
            TipoUsuario = request.Role,
            EsActivo = false // Inicialmente inactivo
        };

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(new ErrorResponseDto(result.Errors.First().Description));

            await _userManager.AddToRoleAsync(user, request.Role);

            if (request.Role == "Cliente")
            {
                var rnd = new Random();
                string numeroCuenta;
                do
                {
                    numeroCuenta = rnd.Next(100000000, 999999999).ToString();
                } while (await _context.Set<CuentaAhorro>().AnyAsync(c => c.NumeroCuenta == numeroCuenta));

                var cuenta = new CuentaAhorro
                {
                    NumeroCuenta = numeroCuenta,
                    ClienteId = user.Id,
                    Balance = request.InitialAmount ?? 0.00m,
                    TipoCuenta = "Principal",
                    Estado = "Activa"
                };

                _context.Set<CuentaAhorro>().Add(cuenta);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var emailBody = $"Hola {user.Nombre},\nSu cuenta ha sido creada correctamente en Artemis Banking.\nUtilice el siguiente token para activar su cuenta:\n{token}";
            await _emailService.SendEmailAsync(user.Email, "Token de activación de cuenta", emailBody);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return StatusCode(201, new
        {
            id = user.Id.ToString(),
            userName = user.UserName,
            email = user.Email,
            role = request.Role,
            isActive = user.EsActivo
        });
    }

    [HttpPost("commerce/{commerceId}")]
    public async Task<IActionResult> CreateCommerceUser(int commerceId, [FromBody] CreateCommerceUserRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponseDto("Datos faltantes o inválidos."));

        if (request.Password != request.ConfirmPassword)
            return BadRequest(new ErrorResponseDto("Las contraseñas no coinciden."));

        var comercio = await _context.Comercios
            .Include(c => c.ComercioUsuarioRel)
            .FirstOrDefaultAsync(c => c.Id == commerceId);

        if (comercio == null)
            return NotFound(new ErrorResponseDto("El comercio indicado no existe."));

        if (comercio.ComercioUsuarioRel != null)
            return Conflict(new ErrorResponseDto("El comercio ya tiene un usuario asociado."));

        if (await _userManager.FindByNameAsync(request.UserName) != null ||
            await _userManager.FindByEmailAsync(request.Email) != null ||
            await _context.Users.AnyAsync(u => u.Cedula == request.Identification))
        {
            return Conflict(new ErrorResponseDto("El usuario, correo o cédula ya se encuentra registrado."));
        }

        var user = new Usuario
        {
            Nombre = request.FirstName,
            Apellido = request.LastName,
            Cedula = request.Identification,
            Email = request.Email,
            UserName = request.UserName,
            TipoUsuario = "Comercio",
            EsActivo = false
        };

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(new ErrorResponseDto(result.Errors.First().Description));

            await _userManager.AddToRoleAsync(user, "Comercio");

            comercio.ComercioUsuarioRel = new ComercioUsuario
            {
                ComercioId = commerceId,
                UsuarioId = user.Id
            };

            var rnd = new Random();
            string numeroCuenta;
            do
            {
                numeroCuenta = rnd.Next(100000000, 999999999).ToString();
            } while (await _context.Set<CuentaAhorro>().AnyAsync(c => c.NumeroCuenta == numeroCuenta));

            var cuenta = new CuentaAhorro
            {
                NumeroCuenta = numeroCuenta,
                ClienteId = user.Id,
                Balance = request.InitialAmount,
                TipoCuenta = "Principal",
                Estado = "Activa"
            };

            _context.Set<CuentaAhorro>().Add(cuenta);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var emailBody = $"Hola {user.Nombre},\nSu cuenta de comercio ha sido creada.\nUtilice el siguiente token para activar su cuenta:\n{token}";
            await _emailService.SendEmailAsync(user.Email, "Token de activación de cuenta", emailBody);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return StatusCode(201, new
        {
            id = user.Id.ToString(),
            userName = user.UserName,
            email = user.Email,
            role = "Comercio",
            commerceId = commerceId,
            isActive = user.EsActivo
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponseDto("Datos faltantes o inválidos."));

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new ErrorResponseDto("El usuario no existe."));

        if (!string.IsNullOrEmpty(request.Password))
        {
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new ErrorResponseDto("Las contraseñas no coinciden."));
        }

        // Validar unicidad si cambian
        if (await _context.Users.AnyAsync(u => u.UserName == request.UserName && u.Id != user.Id) ||
            await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != user.Id) ||
            await _context.Users.AnyAsync(u => u.Cedula == request.Identification && u.Id != user.Id))
        {
            return Conflict(new ErrorResponseDto("El correo, usuario o cédula ya pertenece a otro usuario."));
        }

        user.Nombre = request.FirstName;
        user.Apellido = request.LastName;
        user.Cedula = request.Identification;
        user.Email = request.Email;
        user.UserName = request.UserName;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return BadRequest(new ErrorResponseDto("Error al actualizar los datos del usuario."));

            if (!string.IsNullOrEmpty(request.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
                if (!passResult.Succeeded)
                    return BadRequest(new ErrorResponseDto("Error al actualizar la contraseña."));
            }

            if (request.AdditionalAmount.HasValue && request.AdditionalAmount.Value > 0)
            {
                var cuentaPrincipal = await _context.Set<CuentaAhorro>()
                    .FirstOrDefaultAsync(c => c.ClienteId == user.Id && c.TipoCuenta == "Principal");

                if (cuentaPrincipal != null)
                {
                    cuentaPrincipal.Balance += request.AdditionalAmount.Value;

                    // Registro de la transacción de tipo Crédito por regla de negocio
                    var transaccion = new Transaccion
                    {
                        CuentaDestinoId = cuentaPrincipal.Id, // ID de la cuenta que recibe el depósito
                        Monto = request.AdditionalAmount.Value,
                        TipoTransaccion = "CRÉDITO",
                        Origen = "DEPÓSITO",
                        Beneficiario = cuentaPrincipal.NumeroCuenta,
                        Estado = "APROBADA",
                        FechaTransaccion = DateTime.Now
                    };

                    _context.Set<Transaccion>().Add(transaccion);
                    _context.Set<CuentaAhorro>().Update(cuentaPrincipal);
                    await _context.SaveChangesAsync();
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeStatusRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponseDto("Body inválido o campo status faltante."));

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id)
        {
            return StatusCode(403, new ErrorResponseDto("El administrador autenticado no puede modificar su propio estado."));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new ErrorResponseDto("El usuario indicado no existe."));

        user.EsActivo = request.Status;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return BadRequest(new ErrorResponseDto("No se pudo actualizar el estado del usuario."));

        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new ErrorResponseDto("El usuario no existe."));

        var roles = await _userManager.GetRolesAsync(user);
        var cuenta = await _context.Set<CuentaAhorro>()
            .FirstOrDefaultAsync(c => c.ClienteId == user.Id && c.TipoCuenta == "Principal");

        ComercioUsuario? rel = null;
        if (user.TipoUsuario == "Comercio")
        {
            rel = await _context.Set<ComercioUsuario>().Include(c => c.Comercio).FirstOrDefaultAsync(c => c.UsuarioId == user.Id);
        }

        return Ok(new
        {
            id = user.Id.ToString(),
            userName = user.UserName,
            identification = user.Cedula,
            firstName = user.Nombre,
            lastName = user.Apellido,
            email = user.Email,
            role = roles.FirstOrDefault() ?? user.TipoUsuario,
            commerceId = rel?.ComercioId,
            isActive = user.EsActivo,
            createdAt = DateTime.Now,
            mainAccount = cuenta != null ? new
            {
                accountNumber = cuenta.NumeroCuenta,
                balance = cuenta.Balance,
                isPrincipal = true,
                status = cuenta.Estado
            } : null
        });
    }
}
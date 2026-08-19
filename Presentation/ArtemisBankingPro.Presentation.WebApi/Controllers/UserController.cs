using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using ArtemisBankingPro.Presentation.WebApi.DTOs;
using ArtemisBankingPro.Presentation.WebApi.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    [HttpPost("commerce/{commerceId}")]
    public async Task<IActionResult> CreateCommerceUser(int commerceId, [FromBody] CreateCommerceUserRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponseDto("Datos faltantes o inválidos."));

        var comercio = await _context.Comercios
            .Include(c => c.ComercioUsuarioRel)
            .FirstOrDefaultAsync(c => c.Id == commerceId);

        if (comercio == null)
            return NotFound(new ErrorResponseDto("El comercio indicado no existe."));

        if (comercio.ComercioUsuarioRel != null)
            return Conflict(new ErrorResponseDto("El comercio ya tiene un usuario asociado."));

        var userExists = await _userManager.FindByNameAsync(request.UserName);
        if (userExists != null)
            return BadRequest(new ErrorResponseDto("El nombre de usuario ya está en uso."));

        var emailExists = await _userManager.FindByEmailAsync(request.Email);
        if (emailExists != null)
            return BadRequest(new ErrorResponseDto("El correo electrónico ya está en uso."));

        var cedulaExists = await _context.Users.AnyAsync(u => u.Cedula == request.Identification);
        if (cedulaExists)
            return BadRequest(new ErrorResponseDto("La cédula ya está registrada."));

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
            {
                return BadRequest(new ErrorResponseDto(result.Errors.First().Description));
            }

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
            await _emailService.SendEmailAsync(user.Email, "Activación de Cuenta de Comercio", $"Su token de activación es: {token}");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return Created("", new { Message = "El usuario de comercio fue creado correctamente." });
    }
}

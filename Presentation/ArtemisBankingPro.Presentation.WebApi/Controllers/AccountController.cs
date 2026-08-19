using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Presentation.WebApi.DTOs;
using ArtemisBankingPro.Presentation.WebApi.DTOs.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AccountController(UserManager<Usuario> userManager, IConfiguration configuration, IEmailService emailService)
    {
        _userManager = userManager;
        _configuration = configuration;
        _emailService = emailService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthenticateRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponseDto("Datos faltantes o inválidos."));

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new ErrorResponseDto("Usuario o contraseña incorrectos, o cuenta inactiva."));
        }

        if (!user.EsActivo)
        {
            return Unauthorized(new ErrorResponseDto("La cuenta debe ser activada."));
        }

        if (user.TipoUsuario != "Administrador" && user.TipoUsuario != "Comercio")
        {
            return StatusCode(403, new ErrorResponseDto("El usuario no tiene un rol permitido para usar la API."));
        }

        var jwtSettings = _configuration.GetSection("Jwt");
        var keyStr = jwtSettings["Key"] ?? "EstaEsUnaLlaveMuySecretaDeAlMenos32Caracteres!!!";
        var key = Encoding.UTF8.GetBytes(keyStr);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.TipoUsuario)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["DurationInMinutes"] ?? "60")),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Ok(new AuthenticateResponseDto
        {
            Jwt = tokenHandler.WriteToken(token)
        });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequestDto request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new ErrorResponseDto("Token vacío o inválido."));

        // Como no recibimos userId, buscamos entre los usuarios inactivos
        var inactiveUsers = _userManager.Users.Where(u => !u.EsActivo).ToList();
        Usuario? matchedUser = null;

        foreach (var u in inactiveUsers)
        {
            var isValid = await _userManager.VerifyUserTokenAsync(u, _userManager.Options.Tokens.EmailConfirmationTokenProvider, UserManager<Usuario>.ConfirmEmailTokenPurpose, request.Token);
            if (isValid)
            {
                matchedUser = u;
                break;
            }
        }

        if (matchedUser == null)
            return BadRequest(new ErrorResponseDto("Token inválido, utilizado o no asociado a un usuario válido."));

        var result = await _userManager.ConfirmEmailAsync(matchedUser, request.Token);
        if (result.Succeeded)
        {
            matchedUser.EsActivo = true;
            await _userManager.UpdateAsync(matchedUser);
            return NoContent();
        }

        return BadRequest(new ErrorResponseDto("Error al confirmar la cuenta."));
    }

    [HttpPost("get-reset-token")]
    public async Task<IActionResult> GetResetToken([FromBody] GetResetTokenRequestDto request)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest(new ErrorResponseDto("Datos inválidos."));

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return BadRequest(new ErrorResponseDto("Usuario no existe o no tiene correo registrado."));

        if (user.TipoUsuario != "Administrador" && user.TipoUsuario != "Comercio")
            return BadRequest(new ErrorResponseDto("Usuario no tiene un rol permitido."));

        user.EsActivo = false;
        await _userManager.UpdateAsync(user);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var emailBody = $@"
Hola {user.Nombre},
Se ha generado un token para restablecer la contraseña de su cuenta.
Token de restablecimiento:
{token}
Utilice este token en el endpoint correspondiente para completar el cambio de contraseña.
Si usted no solicitó este cambio, ignore este mensaje.
";
        await _emailService.SendEmailAsync(user.Email, "Token de restablecimiento de contraseña", emailBody);

        return NoContent();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponseDto("Faltan campos requeridos o las contraseñas no coinciden."));

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return BadRequest(new ErrorResponseDto("Usuario no existe."));

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
        if (result.Succeeded)
        {
            user.EsActivo = true;
            await _userManager.UpdateAsync(user);
            return NoContent();
        }

        return BadRequest(new ErrorResponseDto("Token inválido o expirado."));
    }
}

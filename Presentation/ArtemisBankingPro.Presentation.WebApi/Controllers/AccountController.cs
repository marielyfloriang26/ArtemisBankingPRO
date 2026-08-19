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

    public AccountController(UserManager<Usuario> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequestDto request)
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
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("api/v1/cuentasahorro")]
[Authorize(Roles = "Administrador")]
public class CuentaAhorroController : ControllerBase
{
    // Construir la base del controlador de cuenta de ahorro
    
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Cuenta Ahorro API endpoint" });
    }
}

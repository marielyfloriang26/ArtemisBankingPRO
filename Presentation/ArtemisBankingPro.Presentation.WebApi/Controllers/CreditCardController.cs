using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("api/v1/creditcards")]
[Authorize(Roles = "Administrador")]
public class CreditCardController : ControllerBase
{
    // Construir la base del controlador de tarjeta de crédito
    
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Credit Card API endpoint" });
    }
}

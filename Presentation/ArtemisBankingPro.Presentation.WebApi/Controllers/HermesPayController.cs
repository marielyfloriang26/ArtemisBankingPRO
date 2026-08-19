using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Presentation.WebApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("api/payments")]
public class HermesPayController : ControllerBase
{
    private readonly IComercioService _comercioService;

    public HermesPayController(IComercioService comercioService)
    {
        _comercioService = comercioService;
    }

    [HttpPost]
    [Authorize(Roles = "Comercio")]
    public async Task<IActionResult> ProcessHermesPayment()
    {
        var commerceIdClaim = User.FindFirst("CommerceId")?.Value;
        if (!int.TryParse(commerceIdClaim, out int commerceId))
            return BadRequest(new ErrorResponseDto("No se pudo identificar el comercio."));

        var comercio = await _comercioService.GetComercioDetailsAsync(commerceId);
        if (comercio == null)
            return NotFound(new ErrorResponseDto("El comercio indicado no existe."));

        if (!comercio.EsActivo)
            return BadRequest(new ErrorResponseDto("Un comercio inactivo no puede procesar pagos mediante Hermes Pay."));

        return StatusCode(501, new ErrorResponseDto("Fuera de alcance."));
    }
}

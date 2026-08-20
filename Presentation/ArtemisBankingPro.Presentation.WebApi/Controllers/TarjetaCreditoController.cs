using ArtemisBankingPro.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ArtemisBankingPro.Presentation.WebApi.DTOs;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("api/credit-card")]
[Authorize(Roles = "Administrador")]
public class TarjetaCreditoController : ControllerBase
{
    private readonly ITarjetaCreditoService _tarjetaCreditoService;

    public TarjetaCreditoController(ITarjetaCreditoService tarjetaCreditoService)
    {
        _tarjetaCreditoService = tarjetaCreditoService;
    }

    // GET /api/credit-card
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string status = "activa",
        [FromQuery] string? identification = null)
    {
        if (page <= 0 || pageSize <= 0 || pageSize > 20)
            return BadRequest(new { message = "Los parámetros de paginación son inválidos. pageSize máximo es 20." });

        var (success, message, data) = await _tarjetaCreditoService
            .GetCreditCardsPagedAsync(page, pageSize, status, identification);

        if (!success)
            return BadRequest(new { message });

        return Ok(data);
    }

    // POST /api/credit-card
    [HttpPost]
    public async Task<IActionResult> AssignCard([FromBody] AssignCreditCardRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        var (success, message, data) = await _tarjetaCreditoService
            .AssignCreditCardAsync(request.ClientId, request.CreditLimit, adminId);

        if (!success)
        {
            if (message.Contains("no existe") || message.Contains("no encontrado"))
                return NotFound(new { message });

            if (message.Contains("único"))
                return Conflict(new { message });

            return BadRequest(new { message });
        }
    return StatusCode(StatusCodes.Status201Created, data);
     //   return CreatedAtAction(nameof(GetById), new { id = data.Id }, data);//{ id = ((dynamic)data!).id }, data);
    }

    // GET /api/credit-card/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var (success, message, data) = await _tarjetaCreditoService
            .GetCreditCardDetailsAsync(id);

        if (!success)
            return NotFound(new { message = "La tarjeta indicada no existe." });

        return Ok(data);
    }

    // PATCH /api/credit-card/{id}/limit
    [HttpPatch("{id}/limit")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdateLimit(string id, [FromBody] UpdateCreditLimitRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, message) = await _tarjetaCreditoService
            .UpdateCreditLimitAsync(id, request.CreditLimit);

        if (!success)
        {
            if (message == "Not Found")
                return NotFound(new { message = "La tarjeta indicada no existe." });

            return BadRequest(new { message });
        }

        return NoContent();
    }

    // PATCH /api/credit-card/{id}/cancel
    [HttpPatch("{id}/cancel")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Cancel(string id)
    {
        var (success, message) = await _tarjetaCreditoService
            .CancelCreditCardAsync(id);

        if (!success)
        {
            if (message == "Not Found")
                return NotFound(new { message = "La tarjeta indicada no existe." });

            return BadRequest(new { message });
        }

        return NoContent();
    }
    
}

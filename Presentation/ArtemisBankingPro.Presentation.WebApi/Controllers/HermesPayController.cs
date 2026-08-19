using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Presentation.WebApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("pay")]
public class HermesPayController : ControllerBase
{
    private readonly IMediator _mediator;

    public HermesPayController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("get-transactions/{commerceId}")]
    [Authorize(Roles = "Administrador,Comercio")]
    public async Task<IActionResult> GetTransactions([FromRoute] int commerceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role == "Comercio")
        {
            var commerceIdClaim = User.FindFirst("CommerceId")?.Value;
            if (!int.TryParse(commerceIdClaim, out int tokenCommerceId))
                return Forbid();
            commerceId = tokenCommerceId;
        }

        try
        {
            var result = await _mediator.Send(new ArtemisBankingPro.Application.Features.HermesPay.Queries.GetTransactions.GetTransactionsQuery
            {
                CommerceId = commerceId,
                Page = page,
                PageSize = pageSize
            });
            return Ok(result);
        }
        catch (ArtemisBankingPro.Application.Exceptions.ValidationException ex)
        {
            return BadRequest(new ErrorResponseDto(ex.Errors.FirstOrDefault() ?? "Error de validación."));
        }
        catch (Exception ex)
        {
            if (ex.Message == "404") return NotFound(new ErrorResponseDto("El comercio indicado no existe."));
            if (ex.Message.StartsWith("400:")) return BadRequest(new ErrorResponseDto(ex.Message.Substring(4)));
            if (ex.Message.StartsWith("403:")) return Forbid();
            return StatusCode(500, new ErrorResponseDto("Error interno del servidor."));
        }
    }

    [HttpPost("process-payment/{commerceId}")]
    [Authorize(Roles = "Administrador,Comercio")]
    public async Task<IActionResult> ProcessHermesPayment([FromRoute] int commerceId, [FromBody] ArtemisBankingPro.Application.Features.HermesPay.Commands.ProcessPayment.ProcessPaymentCommand command)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role == "Comercio")
        {
            var commerceIdClaim = User.FindFirst("CommerceId")?.Value;
            if (!int.TryParse(commerceIdClaim, out int tokenCommerceId))
                return Forbid();
            commerceId = tokenCommerceId;
        }

        command.CommerceId = commerceId;

        try
        {
            await _mediator.Send(command);
            return NoContent();
        }
        catch (ArtemisBankingPro.Application.Exceptions.ValidationException ex)
        {
            return BadRequest(new ErrorResponseDto(ex.Errors.FirstOrDefault() ?? "Error de validación."));
        }
        catch (Exception ex)
        {
            if (ex.Message == "404") return NotFound(new ErrorResponseDto("El comercio indicado no existe."));
            if (ex.Message.StartsWith("400:")) return BadRequest(new ErrorResponseDto(ex.Message.Substring(4)));
            if (ex.Message.StartsWith("403:")) return Forbid();
            return StatusCode(500, new ErrorResponseDto("Error interno del servidor."));
        }
    }
}

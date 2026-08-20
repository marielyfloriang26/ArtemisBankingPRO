using System.Security.Claims;
using System.Threading.Tasks;
using ArtemisBankingPro.Application.DTOs;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Presentation.WebApi.DTOs.HermesPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("pay")]
[Authorize(Roles = "Administrador,Comercio")]
public class HermesPayController : ControllerBase
{
    private readonly IHermesPayService _hermesPayService;

    public HermesPayController(IHermesPayService hermesPayService)
    {
        _hermesPayService = hermesPayService;
    }

    [HttpGet("get-transactions/{commerceId}")]
    public async Task<IActionResult> GetTransactions(
        int commerceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "";

        (bool success, string message, int statusCode, object? data) = await _hermesPayService
            .GetCommerceTransactionsAsync(commerceId, userId, userRole, page, pageSize);

        if (!success)
            return StatusCode(statusCode, new { message });

        return Ok(data);
    }

    [HttpPost("process-payment/{commerceId}")]
    public async Task<IActionResult> ProcessPayment(
        int commerceId,
        [FromBody] ProcessPaymentDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "";

        var serviceRequest = new ProcessPaymentRequest
        {
            CardNumber = request.CardNumber,
            MonthExpirationCard = request.MonthExpirationCard,
            YearExpirationCard = request.YearExpirationCard,
            Cvc = request.Cvc,
            TransactionAmount = request.TransactionAmount
        };

        (bool success, string message, int statusCode) = await _hermesPayService
            .ProcessPaymentAsync(commerceId, userId, userRole, serviceRequest);

        if (!success)
            return StatusCode(statusCode, new { message });

        return NoContent();
    }
}
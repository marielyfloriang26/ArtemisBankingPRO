using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Presentation.WebApi.DTOs.CuentasAhorro;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("api/savings-account")]
[Authorize(Roles = "Administrador")]
public class CuentaAhorroController : ControllerBase
{
    private readonly ICuentaAhorroService _cuentaAhorroService;

    public CuentaAhorroController(ICuentaAhorroService cuentaAhorroService)
    {
        _cuentaAhorroService = cuentaAhorroService;
    }

    // GET /api/savings-account
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? identification = null,
        [FromQuery] string status = "activa",
        [FromQuery] string type = "todas")
    {
        // Validaciones de parametros
        if (page <= 0 || pageSize <= 0 || pageSize > 20)
            return BadRequest(new { message = "Los parámetros de paginación son inválidos. pageSize máximo es 20." });

        var valoresStatus = new[] { "activa", "cancelada", "todas" };
        var valoresType = new[] { "principal", "secundaria", "todas" };

        if (!valoresStatus.Contains(status.ToLower()))
            return BadRequest(new { message = "El parámetro status solo puede ser: activa, cancelada o todas." });

        if (!valoresType.Contains(type.ToLower()))
            return BadRequest(new { message = "El parámetro type solo puede ser: principal, secundaria o todas." });

                var (cuentas, total) = await _cuentaAhorroService.GetAllPaginatedAsync(page, pageSize, identification, status, type);

        var data = cuentas.Select(x => new SavingsAccountResponse
        {
            Id = x.Cuenta.Id.ToString(),
            AccountNumber = x.Cuenta.NumeroCuenta,
            ClientId = x.Cuenta.ClienteId.ToString(),
            ClientFullName = x.Cliente != null ? $"{x.Cliente.Nombre} {x.Cliente.Apellido}" : "N/A",
            Identification = x.Cliente?.Cedula ?? "N/A",
            Balance = x.Cuenta.Balance,
            Type = x.Cuenta.TipoCuenta,
            Status = x.Cuenta.Estado,
            CreatedAt = x.Cuenta.FechaCreacion
        }).ToList();

        var respuesta = new PaginatedResponse<SavingsAccountResponse>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            Data = data
        };

        return Ok(respuesta);
    }

    // POST /api/savings-account
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSavingsAccountRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!int.TryParse(request.ClientId, out int clienteId))
            return BadRequest(new { message = "El clientId debe ser un número válido." });

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                var (success, errorMessage, cuentaCreada, clienteCreada) = await _cuentaAhorroService
            .CreateSecondaryAccountAsync(clienteId, request.InitialBalance, adminId);

        if (!success)
        {
            if (errorMessage.Contains("no existe") || errorMessage.Contains("no tiene"))
                return NotFound(new { message = errorMessage });
            if (errorMessage.Contains("único"))
                return Conflict(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        var respuesta = new SavingsAccountResponse
        {
            Id = cuentaCreada!.Id.ToString(),
            AccountNumber = cuentaCreada.NumeroCuenta,
            ClientId = cuentaCreada.ClienteId.ToString(),
            ClientFullName = clienteCreada != null ? $"{clienteCreada.Nombre} {clienteCreada.Apellido}" : "N/A",
            Balance = cuentaCreada.Balance,
            Type = cuentaCreada.TipoCuenta,
            Status = cuentaCreada.Estado,
            CreatedAt = cuentaCreada.FechaCreacion
        };

        return CreatedAtAction(nameof(GetAll), respuesta);
    }

    // GET /api/savings-account/{accountNumber}/transactions
    [HttpGet("{accountNumber}/transactions")]
    public async Task<IActionResult> GetTransacciones(
        string accountNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page <= 0 || pageSize <= 0 || pageSize > 20)
            return BadRequest(new { message = "Los parámetros de paginación son inválidos. pageSize máximo es 20." });

                var (cuenta, cliente, transacciones, total) = await _cuentaAhorroService
            .GetTransaccionesByAccountAsync(accountNumber, page, pageSize);

        if (cuenta == null)
            return NotFound(new { message = "La cuenta indicada no existe." });

        var data = transacciones.Select(t => new TransactionResponse
        {
            Id = t.Id.ToString(),
            Date = t.FechaTransaccion,
            Amount = t.Monto,
            TransactionType = t.TipoTransaccion,
            Origin = t.Origen,
            Beneficiary = t.Beneficiario,
            Status = t.Estado
        }).ToList();

        var respuesta = new
        {
            accountNumber = cuenta.NumeroCuenta,
            clientFullName = cliente != null ? $"{cliente.Nombre} {cliente.Apellido}" : "N/A",
            balance = cuenta.Balance,
            type = cuenta.TipoCuenta,
            status = cuenta.Estado,
            transactions = new PaginatedResponse<TransactionResponse>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                Data = data
            }
        };

        return Ok(respuesta);
    }

    // PATCH /api/savings-account/{accountNumber}/cancel
    [HttpPatch("{accountNumber}/cancel")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Cancel(string accountNumber)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        var (success, errorMessage) = await _cuentaAhorroService
            .CancelSecondaryAccountAsync(accountNumber, adminId);

        if (!success)
        {
            if (errorMessage.Contains("no existe"))
                return NotFound(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return NoContent();
    }
}
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Prestamos;
using ArtemisBankingPro.Presentation.WebApi.DTOs;
using ArtemisBankingPro.Presentation.WebApi.DTOs.Loans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("api/loan")]
[Authorize(Roles = "Administrador")]
public class LoanController : ControllerBase
{
    private static readonly int[] PlazosPermitidos = { 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 };

    private readonly IPrestamoService _prestamoService;

    public LoanController(IPrestamoService prestamoService)
    {
        _prestamoService = prestamoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLoans(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? identification = null)
    {
        if (page <= 0)
            return BadRequest(new ErrorResponseDto("El parámetro page debe ser mayor que cero."));

        if (pageSize <= 0)
            return BadRequest(new ErrorResponseDto("El parámetro pageSize debe ser mayor que cero."));

        if (pageSize > 20)
            return BadRequest(new ErrorResponseDto("El valor máximo permitido para pageSize debe ser 20."));

        string estadoFiltro;
        if (string.IsNullOrEmpty(status))
        {
            estadoFiltro = string.IsNullOrEmpty(identification) ? "Activo" : "Todos";
        }
        else if (status is "activos" or "completados" or "todos")
        {
            estadoFiltro = status switch
            {
                "activos" => "Activo",
                "completados" => "Completado",
                _ => "Todos"
            };
        }
        else
        {
            return BadRequest(new ErrorResponseDto("El parámetro status solo puede tener los valores activos, completados o todos."));
        }

        var prestamos = await _prestamoService.GetAllPrestamosFilteredAsync(identification, estadoFiltro);

        int totalRecords = prestamos.Count;
        int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        var pageItems = prestamos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var result = new PagedResultDto<LoanListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            Data = pageItems.Select(MapToListItem).ToList()
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLoanById(string id)
    {
        if (!int.TryParse(id, out int prestamoId))
            return NotFound(new ErrorResponseDto("El préstamo indicado no existe."));

        var prestamo = await _prestamoService.GetPrestamoDetailsAsync(prestamoId);
        if (prestamo == null)
            return NotFound(new ErrorResponseDto("El préstamo indicado no existe."));

        return Ok(MapToDetail(prestamo));
    }

    [HttpPost]
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanRequestDto request)
    {
        if (request.ClientId is null || request.CapitalAmount is null || request.TermInMonths is null || request.AnnualInterestRate is null)
            return BadRequest(new ErrorResponseDto("Todos los campos son obligatorios, excepto confirmHighRisk."));

        if (!int.TryParse(request.ClientId, out int clienteId))
            return BadRequest(new ErrorResponseDto("El clientId indicado no es válido."));

        if (!PlazosPermitidos.Contains(request.TermInMonths.Value))
            return BadRequest(new ErrorResponseDto("El plazo debe ser uno de los valores permitidos."));

        if (request.CapitalAmount.Value <= 0)
            return BadRequest(new ErrorResponseDto("El monto del préstamo debe ser mayor que cero."));

        if (request.AnnualInterestRate.Value < 0)
            return BadRequest(new ErrorResponseDto("La tasa de interés anual no puede ser negativa."));

        if (!await _prestamoService.ExisteClientePorIdAsync(clienteId))
            return NotFound(new ErrorResponseDto("El cliente indicado no existe."));

        string errorElegibilidad = await _prestamoService.ValidarElegibilidadPrestamoAsync(clienteId);
        if (!string.IsNullOrEmpty(errorElegibilidad))
            return BadRequest(new ErrorResponseDto(errorElegibilidad));

        var riesgo = await _prestamoService.EvaluarRiesgoAsync(clienteId, request.CapitalAmount.Value, request.AnnualInterestRate.Value, request.TermInMonths.Value);
        if (riesgo.EsAltoRiesgo && !request.ConfirmHighRisk)
        {
            bool esRiesgoActual = riesgo.Tipo == TipoRiesgoPrestamo.RiesgoActual;
            return Conflict(new HighRiskConflictDto
            {
                Message = esRiesgoActual
                    ? "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema."
                    : "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema.",
                RiskType = esRiesgoActual ? "CurrentHighRisk" : "ProjectedHighRisk",
                CurrentDebt = riesgo.DeudaActual,
                ProjectedDebt = riesgo.DeudaProyectada,
                AverageDebt = riesgo.DeudaPromedio
            });
        }

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int adminId);

        var model = new SavePrestamoViewModel
        {
            ClienteId = clienteId,
            MontoAprobado = request.CapitalAmount.Value,
            PlazoMeses = request.TermInMonths.Value,
            TasaInteresAnual = request.AnnualInterestRate.Value,
            AdminId = adminId
        };

        string resultado = await _prestamoService.AsignarPrestamoAsync(model);
        if (!string.IsNullOrEmpty(resultado) && !resultado.Contains("no fue posible enviar el correo"))
            return BadRequest(new ErrorResponseDto(resultado));

        var creado = await _prestamoService.GetPrestamoActivoByClienteIdAsync(clienteId);
        if (creado == null)
            return BadRequest(new ErrorResponseDto(resultado));

        var response = new CreateLoanResponseDto
        {
            Id = creado.Id.ToString(),
            LoanNumber = creado.NumeroPrestamo,
            ClientId = creado.ClienteId.ToString(),
            ClientFullName = creado.NombreCliente,
            CapitalAmount = creado.MontoAprobado,
            TermInMonths = creado.PlazoMeses,
            AnnualInterestRate = creado.TasaInteresAnual,
            MonthlyInstallment = creado.Cuotas.FirstOrDefault()?.ValorCuota ?? 0m,
            TotalAmountToPay = creado.Cuotas.Sum(c => c.ValorCuota),
            Status = creado.Estado,
            CreatedAt = creado.FechaCreacion
        };

        return CreatedAtAction(nameof(GetLoanById), new { id = creado.Id }, response);
    }

    [HttpPatch("{id}/rate")]
    public async Task<IActionResult> UpdateRate(string id, [FromBody] UpdateLoanRateRequestDto request)
    {
        if (request?.AnnualInterestRate is null)
            return BadRequest(new ErrorResponseDto("La tasa de interés anual es obligatoria."));

        if (!int.TryParse(id, out int prestamoId) || !await _prestamoService.ExistePrestamoAsync(prestamoId))
            return NotFound(new ErrorResponseDto("El préstamo indicado no existe."));

        var model = new EditTasaPrestamoViewModel { PrestamoId = prestamoId, NuevaTasaInteresAnual = request.AnnualInterestRate.Value };
        string resultado = await _prestamoService.EditTasaInteresAsync(model);
        if (!string.IsNullOrEmpty(resultado))
            return BadRequest(new ErrorResponseDto(resultado));

        return NoContent();
    }

    private static LoanListItemDto MapToListItem(PrestamoViewModel p) => new()
    {
        Id = p.Id.ToString(),
        LoanNumber = p.NumeroPrestamo,
        ClientId = p.ClienteId.ToString(),
        ClientFullName = p.NombreCliente,
        CapitalAmount = p.MontoAprobado,
        TotalInstallments = p.PlazoMeses,
        PaidInstallments = p.Cuotas.Count(c => c.EstadoPago == "Pagada"),
        PendingAmount = p.MontoPendiente,
        AnnualInterestRate = p.TasaInteresAnual,
        TermInMonths = p.PlazoMeses,
        Status = p.Estado,
        ClientPaymentStatus = p.EstadoCliente,
        CreatedAt = p.FechaCreacion
    };

    private static LoanDetailDto MapToDetail(PrestamoViewModel p)
    {
        return new LoanDetailDto
        {
            Id = p.Id.ToString(),
            LoanNumber = p.NumeroPrestamo,
            ClientId = p.ClienteId.ToString(),
            ClientFullName = p.NombreCliente,
            CapitalAmount = p.MontoAprobado,
            AnnualInterestRate = p.TasaInteresAnual,
            TermInMonths = p.PlazoMeses,
            MonthlyInstallment = p.Cuotas.FirstOrDefault()?.ValorCuota ?? 0m,
            PendingAmount = p.MontoPendiente,
            Status = p.Estado,
            ClientPaymentStatus = p.EstadoCliente,
            CreatedAt = p.FechaCreacion,
            Amortization = p.Cuotas.Select(c => new AmortizationInstallmentDto
            {
                InstallmentNumber = c.NumeroCuota,
                DueDate = DateOnly.FromDateTime(c.FechaVencimiento),
                InstallmentAmount = c.ValorCuota,
                InterestAmount = c.MontoInteres,
                CapitalAmount = c.MontoCapital,
                PendingInstallmentAmount = c.SaldoPendiente,
                PaymentStatus = c.EstadoPago,
                IsLate = c.TieneAtraso
            }).ToList()
        };
    }
}

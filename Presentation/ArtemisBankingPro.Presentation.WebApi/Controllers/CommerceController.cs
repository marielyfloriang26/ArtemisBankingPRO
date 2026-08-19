using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Comercios;
using ArtemisBankingPro.Presentation.WebApi.DTOs;
using ArtemisBankingPro.Presentation.WebApi.DTOs.Commerces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArtemisBankingPro.Presentation.WebApi.Controllers;

[ApiController]
[Route("api/commerce")]
[Authorize(Roles = "Administrador")]
public class CommerceController : ControllerBase
{
    private readonly IComercioService _comercioService;

    public CommerceController(IComercioService comercioService)
    {
        _comercioService = comercioService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCommerces(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        if (page <= 0)
            return BadRequest(new ErrorResponseDto("El parámetro page debe ser mayor que cero."));

        if (pageSize <= 0)
            return BadRequest(new ErrorResponseDto("El parámetro pageSize debe ser mayor que cero."));

        if (pageSize > 20)
            return BadRequest(new ErrorResponseDto("El valor máximo permitido para pageSize debe ser 20."));

        string estadoFiltro;
        if (string.IsNullOrWhiteSpace(status))
        {
            estadoFiltro = "activo";
        }
        else if (status is "activo" or "inactivo" or "todos")
        {
            estadoFiltro = status;
        }
        else
        {
            return BadRequest(new ErrorResponseDto("El parámetro status solo puede tener los valores activo, inactivo o todos."));
        }

        var comercios = await _comercioService.GetAllComerciosFilteredAsync(estadoFiltro);

        int totalRecords = comercios.Count;
        int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        long omitir = (long)(page - 1) * pageSize;
        var pageItems = omitir >= totalRecords
            ? new List<ComercioViewModel>()
            : comercios.Skip((int)omitir).Take(pageSize).ToList();

        var result = new PagedResultDto<CommerceListItemDto>
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
    public async Task<IActionResult> GetCommerceById(string id)
    {
        if (!int.TryParse(id, out int comercioId))
            return NotFound(new ErrorResponseDto("El comercio indicado no existe."));

        var comercio = await _comercioService.GetComercioDetailsAsync(comercioId);
        if (comercio == null)
            return NotFound(new ErrorResponseDto("El comercio indicado no existe."));

        return Ok(MapToDetail(comercio));
    }

    [HttpPost]
    public async Task<IActionResult> CreateCommerce([FromBody] CreateCommerceRequestDto request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int adminId))
            return Unauthorized(new ErrorResponseDto("Token ausente, inválido o expirado."));

        var model = new SaveComercioViewModel
        {
            Nombre = request.Name,
            Descripcion = request.Description,
            Correo = request.Email,
            Telefono = request.PhoneNumber,
            RNC = request.Rnc,
            AdminId = adminId
        };

        var resultado = await _comercioService.CrearComercioAsync(model);
        if (!resultado.EsExitoso)
            return MapFallo(resultado);

        var creado = resultado.Comercio!;
        var response = new CreateCommerceResponseDto
        {
            Id = creado.Id,
            Name = creado.Nombre,
            Description = creado.Descripcion,
            Email = creado.Correo,
            PhoneNumber = creado.Telefono,
            Rnc = creado.RNC,
            IsActive = creado.EsActivo,
            CreatedAt = creado.FechaCreacion
        };

        return CreatedAtAction(nameof(GetCommerceById), new { id = creado.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCommerce(string id, [FromBody] UpdateCommerceRequestDto request)
    {
        if (!int.TryParse(id, out int comercioId))
            return NotFound(new ErrorResponseDto("El comercio indicado no existe."));

        var model = new SaveComercioViewModel
        {
            Nombre = request.Name,
            Descripcion = request.Description,
            Correo = request.Email,
            Telefono = request.PhoneNumber,
            RNC = request.Rnc
        };

        var resultado = await _comercioService.ActualizarComercioAsync(comercioId, model);
        if (!resultado.EsExitoso)
            return MapFallo(resultado);

        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateCommerceStatus(string id, [FromBody] UpdateCommerceStatusRequestDto request)
    {
        if (!int.TryParse(id, out int comercioId))
            return NotFound(new ErrorResponseDto("El comercio indicado no existe."));

        // La existencia del comercio se valida antes que el campo status, según el orden de las Reglas del PDF.
        var resultado = await _comercioService.CambiarEstadoComercioAsync(comercioId, request?.Status);
        if (!resultado.EsExitoso)
            return MapFallo(resultado);

        return NoContent();
    }

    private IActionResult MapFallo(ComercioOperacionResultado resultado) => resultado.Estado switch
    {
        ComercioOperacionEstado.NoEncontrado => NotFound(new ErrorResponseDto(resultado.Mensaje)),
        ComercioOperacionEstado.Conflicto => Conflict(new ErrorResponseDto(resultado.Mensaje)),
        ComercioOperacionEstado.DatosInvalidos => BadRequest(new ErrorResponseDto(resultado.Mensaje)),
        _ => throw new InvalidOperationException($"Estado de operación no esperado: {resultado.Estado}.")
    };

    private static CommerceListItemDto MapToListItem(ComercioViewModel c) => new()
    {
        Id = c.Id,
        Name = c.Nombre,
        Description = c.Descripcion,
        Email = c.Correo,
        PhoneNumber = c.Telefono,
        Rnc = c.RNC,
        IsActive = c.EsActivo,
        HasAssociatedUser = c.TieneUsuarioAsociado,
        CreatedAt = c.FechaCreacion
    };

    private static CommerceDetailDto MapToDetail(ComercioDetalleViewModel c) => new()
    {
        Id = c.Id,
        Name = c.Nombre,
        Description = c.Descripcion,
        Email = c.Correo,
        PhoneNumber = c.Telefono,
        Rnc = c.RNC,
        IsActive = c.EsActivo,
        CreatedAt = c.FechaCreacion,
        AssociatedUser = c.UsuarioAsociado == null ? null : new AssociatedUserDto
        {
            Id = c.UsuarioAsociado.Id.ToString(),
            UserName = c.UsuarioAsociado.UserName,
            Email = c.UsuarioAsociado.Correo,
            IsActive = c.UsuarioAsociado.EsActivo
        }
    };
}

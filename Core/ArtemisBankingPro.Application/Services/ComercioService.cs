using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Comercios;
using ArtemisBankingPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Services;

public class ComercioService : IComercioService
{
    private static readonly EmailAddressAttribute CorreoValidator = new();

    private readonly IComercioRepository _comercioRepo;
    private readonly IUsuarioRepository _usuarioRepo;

    public ComercioService(IComercioRepository comercioRepo, IUsuarioRepository usuarioRepo)
    {
        _comercioRepo = comercioRepo;
        _usuarioRepo = usuarioRepo;
    }

    public async Task<List<ComercioViewModel>> GetAllComerciosFilteredAsync(string estadoFiltro)
    {
        var comercios = await _comercioRepo.GetAllWithIncludesAsync();

        if (estadoFiltro == "activo")
            comercios = comercios.Where(c => c.EsActivo).ToList();
        else if (estadoFiltro == "inactivo")
            comercios = comercios.Where(c => !c.EsActivo).ToList();

        return comercios
            .OrderByDescending(c => c.FechaCreacion)
            .Select(MapToViewModel)
            .ToList();
    }

    public async Task<ComercioDetalleViewModel?> GetComercioDetailsAsync(int id)
    {
        var comercio = await _comercioRepo.GetByIdWithIncludesAsync(id);
        if (comercio == null) return null;

        var usuarioAsociado = comercio.ComercioUsuarioRel?.Usuario;

        return new ComercioDetalleViewModel
        {
            Id = comercio.Id,
            Nombre = comercio.Nombre,
            Descripcion = comercio.Descripcion,
            Correo = comercio.Correo,
            Telefono = comercio.Telefono,
            RNC = comercio.RNC,
            EsActivo = comercio.EsActivo,
            FechaCreacion = comercio.FechaCreacion,
            UsuarioAsociado = usuarioAsociado == null ? null : new UsuarioAsociadoViewModel
            {
                Id = usuarioAsociado.Id,
                UserName = usuarioAsociado.UserName ?? "",
                Correo = usuarioAsociado.Email ?? "",
                EsActivo = usuarioAsociado.EsActivo
            }
        };
    }

    public async Task<ComercioOperacionResultado> CrearComercioAsync(SaveComercioViewModel model)
    {
        string errorDatos = ValidarDatos(model);
        if (!string.IsNullOrEmpty(errorDatos))
            return ComercioOperacionResultado.Fallo(ComercioOperacionEstado.DatosInvalidos, errorDatos);

        if (await _comercioRepo.ExisteRncAsync(model.RNC!.Trim()) || await _comercioRepo.ExisteCorreoAsync(model.Correo!.Trim()))
            return ComercioOperacionResultado.Fallo(ComercioOperacionEstado.Conflicto, "Ya existe un comercio con el mismo RNC o correo electrónico.");

        var comercio = new Comercio
        {
            Nombre = model.Nombre!.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim(),
            Correo = model.Correo!.Trim(),
            Telefono = model.Telefono!.Trim(),
            RNC = model.RNC!.Trim(),
            EsActivo = true,
            AdminId = model.AdminId,
            // Se guarda sin Kind para que el 201 y las lecturas posteriores serialicen igual.
            FechaCreacion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        try
        {
            await _comercioRepo.AddAsync(comercio);
        }
        catch (DbUpdateException)
        {
            // Los índices únicos de RNC y correo son la última línea de defensa ante concurrencia.
            return ComercioOperacionResultado.Fallo(ComercioOperacionEstado.Conflicto, "Ya existe un comercio con el mismo RNC o correo electrónico.");
        }

        return ComercioOperacionResultado.Exito(MapToViewModel(comercio));
    }

    public async Task<ComercioOperacionResultado> ActualizarComercioAsync(int id, SaveComercioViewModel model)
    {
        var comercio = await _comercioRepo.GetByIdAsync(id);
        if (comercio == null)
            return ComercioOperacionResultado.Fallo(ComercioOperacionEstado.NoEncontrado, "El comercio indicado no existe.");

        string errorDatos = ValidarDatos(model);
        if (!string.IsNullOrEmpty(errorDatos))
            return ComercioOperacionResultado.Fallo(ComercioOperacionEstado.DatosInvalidos, errorDatos);

        if (await _comercioRepo.ExisteRncAsync(model.RNC!.Trim(), id) || await _comercioRepo.ExisteCorreoAsync(model.Correo!.Trim(), id))
            return ComercioOperacionResultado.Fallo(ComercioOperacionEstado.Conflicto, "El RNC o correo electrónico pertenece a otro comercio.");

        // El estado, el administrador responsable y la fecha de creación no se modifican desde este endpoint.
        comercio.Nombre = model.Nombre!.Trim();
        comercio.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim();
        comercio.Correo = model.Correo!.Trim();
        comercio.Telefono = model.Telefono!.Trim();
        comercio.RNC = model.RNC!.Trim();

        try
        {
            await _comercioRepo.UpdateAsync(comercio, comercio.Id);
        }
        catch (DbUpdateException)
        {
            return ComercioOperacionResultado.Fallo(ComercioOperacionEstado.Conflicto, "El RNC o correo electrónico pertenece a otro comercio.");
        }

        return ComercioOperacionResultado.Exito();
    }

    public async Task<ComercioOperacionResultado> CambiarEstadoComercioAsync(int id, bool? estado)
    {
        var comercio = await _comercioRepo.GetByIdAsync(id);
        if (comercio == null)
            return ComercioOperacionResultado.Fallo(ComercioOperacionEstado.NoEncontrado, "El comercio indicado no existe.");

        if (estado is null)
            return ComercioOperacionResultado.Fallo(ComercioOperacionEstado.DatosInvalidos, "El campo status es obligatorio.");

        comercio.EsActivo = estado.Value;
        await _comercioRepo.UpdateAsync(comercio, comercio.Id);

        // Al desactivar un comercio, sus usuarios asociados deben quedar inactivos.
        // Al reactivarlo, deben permanecer inactivos hasta completar el restablecimiento de contraseña.
        if (!estado.Value)
        {
            var usuarios = await _comercioRepo.GetUsuariosAsociadosAsync(id);
            foreach (var usuario in usuarios.Where(u => u.EsActivo))
            {
                usuario.EsActivo = false;
                await _usuarioRepo.UpdateAsync(usuario, usuario.Id);
            }
        }

        return ComercioOperacionResultado.Exito();
    }

    private static string ValidarDatos(SaveComercioViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Nombre)) return "El nombre del comercio es obligatorio.";
        if (string.IsNullOrWhiteSpace(model.Correo)) return "El correo electrónico es obligatorio.";
        if (!CorreoValidator.IsValid(model.Correo)) return "El correo electrónico debe tener un formato válido.";
        if (string.IsNullOrWhiteSpace(model.Telefono)) return "El teléfono es obligatorio.";
        if (string.IsNullOrWhiteSpace(model.RNC)) return "El RNC es obligatorio.";

        // Longitudes máximas impuestas por el esquema; sin ellas la escritura falla en base de datos.
        if (model.Nombre!.Trim().Length > 100
            || (model.Descripcion?.Trim().Length ?? 0) > 255
            || model.Correo!.Trim().Length > 100
            || model.Telefono!.Trim().Length > 20
            || model.RNC!.Trim().Length > 20)
            return "Datos faltantes o inválidos.";

        return string.Empty;
    }

    private static ComercioViewModel MapToViewModel(Comercio c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Descripcion = c.Descripcion,
        Correo = c.Correo,
        Telefono = c.Telefono,
        RNC = c.RNC,
        EsActivo = c.EsActivo,
        TieneUsuarioAsociado = c.ComercioUsuarioRel != null,
        FechaCreacion = c.FechaCreacion
    };
}

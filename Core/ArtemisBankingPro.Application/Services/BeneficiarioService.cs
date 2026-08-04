using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Beneficiarios;
using ArtemisBankingPro.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Services;

public class BeneficiarioService : IBeneficiarioService
{
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly ICuentaAhorroRepository _cuentaAhorroRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public BeneficiarioService(
        IBeneficiarioRepository beneficiarioRepository,
        ICuentaAhorroRepository cuentaAhorroRepository,
        IUsuarioRepository usuarioRepository)
    {
        _beneficiarioRepository = beneficiarioRepository;
        _cuentaAhorroRepository = cuentaAhorroRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<List<BeneficiarioViewModel>> GetAllByClienteIdAsync(int clienteId)
    {
        var beneficiarios = await _beneficiarioRepository.GetAllByClienteIdAsync(clienteId);
        return beneficiarios.Select(b => new BeneficiarioViewModel
        {
            Id = b.Id,
            Nombre = b.Nombre,
            Apellido = b.Apellido,
            NumeroCuenta = b.CuentaAhorro!.NumeroCuenta
        }).ToList();
    }

    public async Task<string?> AddBeneficiarioAsync(int clienteId, SaveBeneficiarioViewModel vm)
    {
        // Validación: El número de cuenta ingresado no corresponde a una cuenta válida.
        var cuenta = await _cuentaAhorroRepository.GetByNumeroCuentaAsync(vm.NumeroCuenta);
        if (cuenta == null)
        {
            return "El número de cuenta ingresado no corresponde a una cuenta válida.";
        }

        // Validación: No puede agregar una cuenta cancelada como beneficiario.
        if (cuenta.Estado == "Cancelada")
        {
            return "No puede agregar una cuenta cancelada como beneficiario.";
        }

        // Validación: No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas.
        if (cuenta.ClienteId == clienteId)
        {
            return "No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas.";
        }

        // Validación: Esta cuenta ya se encuentra registrada como beneficiario.
        var existingBeneficiario = await _beneficiarioRepository.GetByClienteAndCuentaIdAsync(clienteId, cuenta.Id);
        if (existingBeneficiario != null)
        {
            return "Esta cuenta ya se encuentra registrada como beneficiario.";
        }

        var propietario = await _usuarioRepository.GetByIdAsync(cuenta.ClienteId);
        if (propietario == null)
        {
            return "El número de cuenta ingresado no corresponde a una cuenta válida.";
        }

        var nuevoBeneficiario = new Beneficiario
        {
            ClienteId = clienteId,
            CuentaAhorroId = cuenta.Id,
            Nombre = propietario.Nombre,
            Apellido = propietario.Apellido
        };

        await _beneficiarioRepository.AddAsync(nuevoBeneficiario);
        return null; // Éxito
    }

    public async Task<bool> DeleteBeneficiarioAsync(int clienteId, int beneficiarioId)
    {
        var beneficiario = await _beneficiarioRepository.GetByIdAsync(beneficiarioId);
        if (beneficiario != null && beneficiario.ClienteId == clienteId)
        {
            await _beneficiarioRepository.DeleteAsync(beneficiario);
            return true;
        }
        return false;
    }
}

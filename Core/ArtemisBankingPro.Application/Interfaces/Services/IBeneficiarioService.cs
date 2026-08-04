using ArtemisBankingPro.Application.ViewModels.Beneficiarios;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Services;

public interface IBeneficiarioService
{
    Task<List<BeneficiarioViewModel>> GetAllByClienteIdAsync(int clienteId);
    Task<string?> AddBeneficiarioAsync(int clienteId, SaveBeneficiarioViewModel vm);
    Task<bool> DeleteBeneficiarioAsync(int clienteId, int beneficiarioId);
}

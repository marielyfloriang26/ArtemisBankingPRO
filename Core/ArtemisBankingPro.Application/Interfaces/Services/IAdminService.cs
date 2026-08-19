using System.Threading.Tasks;
using ArtemisBankingPro.Application.ViewModels.Admin;

namespace ArtemisBankingPro.Application.Interfaces.Services;

public interface IAdminService
{
    Task<AdminHomeViewModel> GetDashboardIndicatorsAsync();
}
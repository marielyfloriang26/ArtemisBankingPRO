using ArtemisBankingPro.Domain.Entities;

namespace ArtemisBankingPro.Application.ViewModels.CuentaAhorro;

public class ClientDebtViewModel
{
    public Usuario Cliente { get; set; } = null!;
    public decimal MontoTotalDeuda { get; set; }
}

namespace ArtemisBankingPro.Application.ViewModels.Admin;

public class AdminHomeViewModel
{
    public int TotalTransaccionesHistoricas { get; set; }
    public int TransaccionesDelDia { get; set; }
    
    public int TotalPagosHistoricos { get; set; }
    public int PagosDelDia { get; set; }
    
    public int ClientesActivos { get; set; }
    public int ClientesInactivos { get; set; }
    
    public int TotalProductosFinancierosActivos { get; set; }
    public int PrestamosVigentes { get; set; }
    public int TarjetasCreditoActivas { get; set; }
    public int CuentasAhorroActivas { get; set; }
    
    public decimal MontoPromedioDeudaPorCliente { get; set; }
}
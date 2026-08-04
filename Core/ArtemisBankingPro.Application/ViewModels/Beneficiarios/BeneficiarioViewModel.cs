namespace ArtemisBankingPro.Application.ViewModels.Beneficiarios;

public class BeneficiarioViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string NumeroCuenta { get; set; } = null!;
}

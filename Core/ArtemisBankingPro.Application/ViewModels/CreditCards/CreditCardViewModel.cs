namespace ArtemisBankingPro.Application.ViewModels.CreditCards;

public class CreditCardViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal LimiteCredito { get; set; }
    public decimal TasaInteres { get; set; }
    public decimal CostoEmision { get; set; }
    public string? Descripcion { get; set; }
    public string Estado { get; set; } = "Activa";
}

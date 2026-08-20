namespace ArtemisBankingPro.Application.ViewModels.Cliente
{
    public class TransaccionDetalleViewModel
    {
        public DateTime FechaTransaccion { get; set; }
        public decimal Monto { get; set; }
        public string TipoTransaccion { get; set; } = string.Empty;
        public string Beneficiario { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}
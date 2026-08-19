namespace ArtemisBankingPro.Application.ViewModels.Cliente
{
    public class CuentaAhorroViewModel
    {
        public int Id { get; set; }
        public string NumeroCuenta { get; set; } = null!;
        public decimal Balance { get; set; }
        public string TipoCuenta { get; set; } = null!; // "Principal" o "Secundaria"
        public string Estado { get; set; } = null!;
        public int ClienteId { get; set; }

        // Propiedad de conveniencia para mostrar en el dropdown del formulario
        public string DescripcionDropdown => $"Cuenta No. {NumeroCuenta} ({TipoCuenta}) - Balance: RD$ {Balance:N2}";
    }
}
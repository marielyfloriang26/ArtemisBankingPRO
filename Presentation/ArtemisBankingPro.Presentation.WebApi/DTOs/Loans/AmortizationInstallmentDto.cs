namespace ArtemisBankingPro.Presentation.WebApi.DTOs.Loans;

public class AmortizationInstallmentDto
{
    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal CapitalAmount { get; set; }
    public decimal PendingInstallmentAmount { get; set; }
    public string PaymentStatus { get; set; } = null!;
    public bool IsLate { get; set; }
}

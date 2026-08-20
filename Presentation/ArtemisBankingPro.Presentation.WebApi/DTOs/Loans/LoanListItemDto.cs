namespace ArtemisBankingPro.Presentation.WebApi.DTOs.Loans;

public class LoanListItemDto
{
    public string Id { get; set; } = null!;
    public string LoanNumber { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientFullName { get; set; } = null!;
    public decimal CapitalAmount { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public int TermInMonths { get; set; }
    public string Status { get; set; } = null!;
    public string ClientPaymentStatus { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

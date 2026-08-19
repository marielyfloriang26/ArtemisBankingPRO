namespace ArtemisBankingPro.Presentation.WebApi.DTOs.Loans;

public class CreateLoanRequestDto
{
    public string? ClientId { get; set; }
    public decimal? CapitalAmount { get; set; }
    public int? TermInMonths { get; set; }
    public decimal? AnnualInterestRate { get; set; }
    public bool ConfirmHighRisk { get; set; } = false;
}

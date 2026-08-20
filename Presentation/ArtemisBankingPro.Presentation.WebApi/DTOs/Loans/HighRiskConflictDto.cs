namespace ArtemisBankingPro.Presentation.WebApi.DTOs.Loans;

public class HighRiskConflictDto
{
    public string Message { get; set; } = null!;
    public string RiskType { get; set; } = null!;
    public decimal CurrentDebt { get; set; }
    public decimal ProjectedDebt { get; set; }
    public decimal AverageDebt { get; set; }
}

using System;

namespace ArtemisBankingPro.Presentation.WebApi.DTOs.CuentasAhorro;

public class TransactionResponse
{
    public string Id { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = null!;
    public string Origin { get; set; } = null!;
    public string Beneficiary { get; set; } = null!;
    public string Status { get; set; } = null!;
}
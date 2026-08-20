using System;

namespace ArtemisBankingPro.Presentation.WebApi.DTOs.CuentasAhorro;

public class SavingsAccountResponse
{
    public string Id { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string ClientFullName { get; set; } = null!;
    public string Identification { get; set; } = null!;
    public decimal Balance { get; set; }
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
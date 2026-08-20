using System.Collections.Generic;

namespace ArtemisBankingPro.Presentation.WebApi.DTOs;

public class CreditCardDetailDto : CreditCardDto
{
    public List<ConsumptionDto> Consumptions { get; set; } = new();
}

public class ConsumptionDto
{
    public string Id { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string CommerceName { get; set; } = null!;
    public string Status { get; set; } = null!;
}
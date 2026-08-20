using System.Text.Json.Serialization;

namespace ArtemisBankingPro.Presentation.WebApi.DTOs.HermesPay;

public class ProcessPaymentDto
{
    [JsonPropertyName("cardNumber")]
    public string CardNumber { get; set; } = string.Empty;

    [JsonPropertyName("monthExpirationCard")]
    public string MonthExpirationCard { get; set; } = string.Empty;

    [JsonPropertyName("yearExpirationCard")]
    public string YearExpirationCard { get; set; } = string.Empty;

    [JsonPropertyName("cvc")]
    public string Cvc { get; set; } = string.Empty;

    [JsonPropertyName("transactionAmount")]
    public decimal TransactionAmount { get; set; }
}
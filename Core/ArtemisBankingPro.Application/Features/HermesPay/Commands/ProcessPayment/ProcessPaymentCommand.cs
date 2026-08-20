using MediatR;

namespace ArtemisBankingPro.Application.Features.HermesPay.Commands.ProcessPayment;

public class ProcessPaymentCommand : IRequest<Unit>
{
    public int CommerceId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string MonthExpirationCard { get; set; } = null!;
    public string YearExpirationCard { get; set; } = null!;
    public string Cvc { get; set; } = null!;
    public decimal TransactionAmount { get; set; }
}

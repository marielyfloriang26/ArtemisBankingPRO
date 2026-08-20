using FluentValidation;

namespace ArtemisBankingPro.Application.Features.HermesPay.Commands.ProcessPayment;

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(p => p.CardNumber)
            .NotEmpty().WithMessage("El número de tarjeta es requerido.")
            .Length(16).WithMessage("El número de tarjeta debe contener exactamente 16 dígitos.")
            .Matches(@"^\d{16}$").WithMessage("El número de tarjeta debe contener exactamente 16 dígitos.");

        RuleFor(p => p.MonthExpirationCard)
            .NotEmpty().WithMessage("El mes de expiración es requerido.")
            .Matches(@"^(0[1-9]|1[0-2])$").WithMessage("El mes de expiración debe tener un valor válido entre 01 y 12.");

        RuleFor(p => p.YearExpirationCard)
            .NotEmpty().WithMessage("El año de expiración es requerido.");

        RuleFor(p => p.Cvc)
            .NotEmpty().WithMessage("El CVC es requerido.")
            .Length(3).WithMessage("El CVC debe contener exactamente 3 dígitos.")
            .Matches(@"^\d{3}$").WithMessage("El CVC debe contener exactamente 3 dígitos.");

        RuleFor(p => p.TransactionAmount)
            .GreaterThan(0).WithMessage("El monto de la transacción debe ser mayor que cero.");
    }
}

using FluentValidation;

namespace ArtemisBankingPro.Application.Features.HermesPay.Queries.GetTransactions;

public class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    public GetTransactionsQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThan(0).WithMessage("El parámetro page debe ser mayor que cero.");

        RuleFor(q => q.PageSize)
            .GreaterThan(0).WithMessage("El parámetro pageSize debe ser mayor que cero.")
            .LessThanOrEqualTo(20).WithMessage("El valor máximo permitido para pageSize debe ser 20.");
    }
}

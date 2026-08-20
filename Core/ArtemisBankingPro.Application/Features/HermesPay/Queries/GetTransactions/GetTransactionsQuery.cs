using MediatR;
using System.Collections.Generic;

namespace ArtemisBankingPro.Application.Features.HermesPay.Queries.GetTransactions;

public class GetTransactionsQuery : IRequest<PaginatedResult<TransactionDto>>
{
    public int CommerceId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PaginatedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public int CommerceId { get; set; }
    public string CommerceName { get; set; } = null!;
    public List<T> Data { get; set; } = new();
}

public class TransactionDto
{
    public string Id { get; set; } = null!;
    public string TransactionDate { get; set; } = null!;
    public decimal Amount { get; set; }
    public string CardLastFourDigits { get; set; } = null!;
    public string Status { get; set; } = null!;
}

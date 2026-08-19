using System.Collections.Generic;

namespace ArtemisBankingPro.Application.DTOs;

public class PaginatedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public List<T> Data { get; set; } = new List<T>();
}
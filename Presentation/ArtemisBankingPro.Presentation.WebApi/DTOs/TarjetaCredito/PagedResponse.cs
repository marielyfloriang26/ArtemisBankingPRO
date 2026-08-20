using System.Collections.Generic;

namespace ArtemisBankingPro.Presentation.WebApi.DTOs;

public class PagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public List<T> Data { get; set; } = new();
}
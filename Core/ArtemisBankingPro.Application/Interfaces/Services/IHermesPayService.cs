using System.Threading.Tasks;
using ArtemisBankingPro.Application.DTOs;

namespace ArtemisBankingPro.Application.Interfaces.Services;

public interface IHermesPayService
{
    Task<(bool Success, string Message, int StatusCode, object? Data)> GetCommerceTransactionsAsync(
        int routeCommerceId, 
        string userId, 
        string userRole, 
        int page, 
        int pageSize);

    Task<(bool Success, string Message, int StatusCode)> ProcessPaymentAsync(
        int routeCommerceId, 
        string userId, 
        string userRole, 
        ProcessPaymentRequest request);
}
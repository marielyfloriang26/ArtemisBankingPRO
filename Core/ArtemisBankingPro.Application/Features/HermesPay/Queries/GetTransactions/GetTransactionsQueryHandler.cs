using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Features.HermesPay.Queries.GetTransactions;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, PaginatedResult<TransactionDto>>
{
    private readonly IComercioRepository _comercioRepository;
    private readonly ICuentaAhorroRepository _cuentaRepository;
    private readonly ITransaccionRepository _transaccionRepository;

    public GetTransactionsQueryHandler(
        IComercioRepository comercioRepository,
        ICuentaAhorroRepository cuentaRepository,
        ITransaccionRepository transaccionRepository)
    {
        _comercioRepository = comercioRepository;
        _cuentaRepository = cuentaRepository;
        _transaccionRepository = transaccionRepository;
    }

    public async Task<PaginatedResult<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var comercio = await _comercioRepository.GetByIdWithIncludesAsync(request.CommerceId);
        if (comercio == null)
            throw new Exception("404");

        if (comercio.ComercioUsuarioRel == null)
            throw new Exception("400:El comercio debe tener un usuario asociado.");

        var cuentaComercio = await _cuentaRepository.GetCuentaPrincipalByUsuarioIdAsync(comercio.ComercioUsuarioRel.UsuarioId);
        if (cuentaComercio == null)
            throw new Exception("400:El usuario de comercio no tiene cuenta de ahorro principal.");

        var transacciones = await _transaccionRepository.GetByCuentaIdAsync(cuentaComercio.Id);
        // Filtrar solo las que sean abonos por Hermes Pay. La transaccion de pago tiene Origen = ultimos4.
        // Wait, "Estas transacciones corresponden a los pagos recibidos en la cuenta de ahorro principal del usuario asociado al comercio."
        // GetByCuentaIdAsync gets transacciones origen or destino? Let's check GetByCuentaIdAsync.
        
        var receivedTransactions = transacciones
            .Where(t => t.CuentaDestinoId == cuentaComercio.Id && t.TipoTransaccion == "CRÉDITO" && t.Estado == "APROBADA")
            .OrderByDescending(t => t.FechaTransaccion)
            .ToList();
            
        int totalRecords = receivedTransactions.Count;
        int totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize);
        
        var pagedData = receivedTransactions
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id.ToString(),
                TransactionDate = t.FechaTransaccion.ToString("s"),
                Amount = t.Monto,
                CardLastFourDigits = t.Origen, // Guardamos los ultimos 4 digitos en Origen
                Status = t.Estado
            })
            .ToList();

        return new PaginatedResult<TransactionDto>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                CommerceId = comercio.Id,
                CommerceName = comercio.Nombre,
                Data = pagedData
            };
    }
}

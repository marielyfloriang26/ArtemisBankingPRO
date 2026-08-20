using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.CreditCards;
using ArtemisBankingPro.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Services;

public class CreditCardService : ICreditCardService
{
    private readonly IProductoTarjetaCreditoRepository _repository;

    public CreditCardService(IProductoTarjetaCreditoRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<CreditCardViewModel>> GetAllViewModel()
    {
        var list = await _repository.GetAllAsync();
        return list.Select(x => new CreditCardViewModel
        {
            Id = x.Id,
            Nombre = x.Nombre,
            LimiteCredito = x.LimiteCredito,
            TasaInteres = x.TasaInteres,
            CostoEmision = x.CostoEmision,
            Descripcion = x.Descripcion,
            Estado = x.Estado
        }).ToList();
    }

    public async Task<SaveCreditCardViewModel> Add(SaveCreditCardViewModel vm)
    {
        var entity = new ProductoTarjetaCredito
        {
            Nombre = vm.Nombre,
            LimiteCredito = vm.LimiteCredito,
            TasaInteres = vm.TasaInteres,
            CostoEmision = vm.CostoEmision,
            Descripcion = vm.Descripcion,
            Estado = "Activa"
        };

        entity = await _repository.AddAsync(entity);

        vm.Id = entity.Id;
        return vm;
    }

    public async Task<SaveCreditCardViewModel> GetByIdSaveViewModel(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return null!;

        return new SaveCreditCardViewModel
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            LimiteCredito = entity.LimiteCredito,
            TasaInteres = entity.TasaInteres,
            CostoEmision = entity.CostoEmision,
            Descripcion = entity.Descripcion
        };
    }

    public async Task Update(SaveCreditCardViewModel vm, int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity != null)
        {
            entity.Nombre = vm.Nombre;
            entity.LimiteCredito = vm.LimiteCredito;
            entity.TasaInteres = vm.TasaInteres;
            entity.CostoEmision = vm.CostoEmision;
            entity.Descripcion = vm.Descripcion;

            await _repository.UpdateAsync(entity, id);
        }
    }

    public async Task ChangeStatus(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity != null)
        {
            entity.Estado = entity.Estado == "Activa" ? "Inactiva" : "Activa";
            await _repository.UpdateAsync(entity, id);
        }
    }

    public async Task<List<dynamic>> GetConsumosByTarjetaIdAsync(int tarjetaId, int clienteId)
    {
        // Como CreditCardService administra el catálogo de productos, 
        // los consumos del cliente operativo deben consultarse desde el servicio o repositorio de tarjetas del cliente.
        // Retornamos una lista vacía de forma segura para que compile y funcione:
        return await Task.FromResult(new List<dynamic>());
}
}

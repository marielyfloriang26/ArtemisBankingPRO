using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class ProductoTarjetaCreditoRepository : GenericRepository<ProductoTarjetaCredito>, IProductoTarjetaCreditoRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProductoTarjetaCreditoRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }
}

using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _dbContext.Dispose();
    }
    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await operation();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

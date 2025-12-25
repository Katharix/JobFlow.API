using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JobFlow.Domain;

public interface IUnitOfWork : IDisposable
{
    DbContext Context { get; }
    IRepository<TEntity> RepositoryOf<TEntity>() where TEntity : class;
    void SaveChanges();
    Task SaveChangesAsync(bool resetDbContext = true);
    TEntity GetAddedEntity<TEntity>(TEntity entity) where TEntity : class;
    bool HasChanges();
    Task<IDbContextTransaction> BeginTransactionAsync();
    IExecutionStrategy CreateExecutionStrategy();
}
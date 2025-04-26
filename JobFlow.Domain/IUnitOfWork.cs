using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Domain
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<TEntity> RepositoryOf<TEntity>() where TEntity : class;
        void SaveChanges();
        Task SaveChangesAsync(bool resetDbContext = true);
        TEntity GetAddedEntity<TEntity>(TEntity entity) where TEntity : class;
        bool HasChanges();
        Task<IDbContextTransaction> BeginTransactionAsync();
        IExecutionStrategy CreateExecutionStrategy();
    }
}

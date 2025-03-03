using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.Persistence
{
    public class JobFlowUnitOfWork : IUnitOfWork
    {
        private readonly ILogger<JobFlowUnitOfWork> _logger;
        private readonly IDbContextFactory<JobFlowDbContext> _factory;
        private JobFlowDbContext _context;

        public JobFlowUnitOfWork(ILogger<JobFlowUnitOfWork> logger, IDbContextFactory<JobFlowDbContext> factory)
        {
            _factory = factory;
            _logger = logger;

        }
        public IRepository<TEntity> RepositoryOf<TEntity>() where TEntity : class
        {
            EnsureDbContext();
            //return new Repository<TEntity>(_context.Set<TEntity>(), _context);
            return new Repository<TEntity>(_context);
        }
        public void SaveChanges()
        {
            if (_context == null)
                return;

            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                _logger.LogError("An unknown error occured saving changes to the database", e);

                throw;
            }

            ResetDbContext();
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            EnsureDbContext();
            return await _context.Database.BeginTransactionAsync();
        }

        public IExecutionStrategy CreateExecutionStrategy()
        {
            EnsureDbContext();
            return _context.Database.CreateExecutionStrategy();
        }


        public async Task SaveChangesAsync(bool resetDbContext = true)
        {
            if (_context == null)
                return;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError("An unknown error occured saving changes to the database", e);

                throw;
            }
            if (resetDbContext)
            {
                ResetDbContext();
            }

        }

        public TEntity GetAddedEntity<TEntity>(TEntity entity) where TEntity : class
        {
            if (_context.Entry(entity).State == EntityState.Added)
            {
                return entity;
            }
            return null;
        }

        public void Dispose()
        {
            ClearDbContext();
        }

        private void ResetDbContext()
        {
            ClearDbContext();
            EnsureDbContext();
        }

        private void ClearDbContext()
        {
            if (_context != null)
            {
                _context.Dispose();
            }
            _context = null;
        }

        private void EnsureDbContext()
        {
            if (_context != null)
                return;

            _context = _factory.CreateDbContext();
        }
        public bool HasChanges()
        {
            return (_context != null) && _context.ChangeTracker.HasChanges();
        }
    }

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

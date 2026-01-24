using JobFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace JobFlow.Infrastructure.Persistence;

public class JobFlowUnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<JobFlowDbContext> _factory;
    private readonly ILogger<JobFlowUnitOfWork> _logger;
    private JobFlowDbContext _context;

    public JobFlowUnitOfWork(ILogger<JobFlowUnitOfWork> logger, IDbContextFactory<JobFlowDbContext> factory)
    {
        _factory = factory;
        _logger = logger;
    }

    public DbContext Context => _context;

    public IRepository<TEntity> RepositoryOf<TEntity>() where TEntity : class
    {
        EnsureDbContext();
        return new Repository<TEntity>(_context);
    }

    DbContext IUnitOfWork.Context => Context;

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
    }

    public TEntity GetAddedEntity<TEntity>(TEntity entity) where TEntity : class
    {
        if (_context.Entry(entity).State == EntityState.Added) return entity;
        return null;
    }

    public void Dispose()
    {
        ClearDbContext();
    }

    public bool HasChanges()
    {
        return _context != null && _context.ChangeTracker.HasChanges();
    }

    private void ResetDbContext()
    {
        ClearDbContext();
        EnsureDbContext();
    }

    private void ClearDbContext()
    {
        if (_context != null) _context.Dispose();
        _context = null;
    }

    private void EnsureDbContext()
    {
        if (_context != null)
            return;

        _context = _factory.CreateDbContext();
    }
}
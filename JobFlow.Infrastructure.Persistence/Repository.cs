using System.Linq.Expressions;
using JobFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Infrastructure.Persistence;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    // 🔹 Query
    public IQueryable<T> Query(Expression<Func<T, bool>> filter = null)
    {
        return filter != null ? _dbSet.Where(filter) : _dbSet;
    }

    public IQueryable<T> QueryWithNoTracking()
    {
        return _dbSet.AsNoTracking();
    }

    // 🔹 Create
    public void Add(T item)
    {
        _dbSet.Add(item);
    }

    public async Task AddAsync(T item)
    {
        await _dbSet.AddAsync(item);
    }

    public void AddRange(IEnumerable<T> items)
    {
        _dbSet.AddRange(items);
    }

    public async Task AddRangeAsync(IEnumerable<T> items)
    {
        await _dbSet.AddRangeAsync(items);
    }

    // 🔹 Update
    public void Update(T item)
    {
        _dbSet.Update(item);
    }

    public Task UpdateAsync(T item)
    {
        _dbSet.Update(item);
        return Task.CompletedTask;
    }

    public void UpdateRange(IEnumerable<T> items)
    {
        _dbSet.UpdateRange(items);
    }

    public Task UpdateRangeAsync(IEnumerable<T> items)
    {
        _dbSet.UpdateRange(items);
        return Task.CompletedTask;
    }

    // 🔹 Delete
    public void Remove(T item)
    {
        _dbSet.Remove(item);
    }

    public Task RemoveAsync(T item)
    {
        _dbSet.Remove(item);
        return Task.CompletedTask;
    }

    public void RemoveRange(IEnumerable<T> items)
    {
        _dbSet.RemoveRange(items);
    }

    public Task RemoveRangeAsync(IEnumerable<T> items)
    {
        _dbSet.RemoveRange(items);
        return Task.CompletedTask;
    }

    // 🔹 Read Helpers
    public async Task<T> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }
}
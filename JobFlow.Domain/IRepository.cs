using System.Linq.Expressions;

namespace JobFlow.Domain;

public interface IRepository<TEntity> where TEntity : class
{
    // 🔹 Query
    IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? filter = null);
    IQueryable<TEntity> QueryWithNoTracking();

    // 🔹 Create
    void Add(TEntity item);
    Task AddAsync(TEntity item);
    void AddRange(IEnumerable<TEntity> items);
    Task AddRangeAsync(IEnumerable<TEntity> items);

    // 🔹 Update
    void Update(TEntity item);
    Task UpdateAsync(TEntity item);
    void UpdateRange(IEnumerable<TEntity> items);
    Task UpdateRangeAsync(IEnumerable<TEntity> items);

    // 🔹 Delete
    void Remove(TEntity item);
    Task RemoveAsync(TEntity item);
    void RemoveRange(IEnumerable<TEntity> items);
    Task RemoveRangeAsync(IEnumerable<TEntity> items);

    // 🔹 Read helpers
    Task<TEntity> GetByIdAsync(Guid id);
    Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);
}
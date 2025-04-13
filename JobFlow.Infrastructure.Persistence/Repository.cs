using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.Persistence
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly DbSet<T> _dbSet;
        private readonly DbContext _context;

        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> Query(Expression<Func<T, bool>> filter = null)
        {
            IQueryable<T> query = _dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return query;
        }

        public IQueryable<T> QueryWithNoTracking()
        {
            return _dbSet.AsNoTracking();
        }

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

        public void Remove(T item)
        {
            _dbSet.Remove(item);
        }

        public void RemoveRange(IEnumerable<T> items)
        {
            _dbSet.RemoveRange(items);
        }

        public void Update(T item)
        {
            _dbSet.Update(item);
        }

        public void UpdateRange(IEnumerable<T> items)
        {
            _dbSet.UpdateRange(items);
        }

    }
}

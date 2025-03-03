using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.Persistence
{
    public interface IRepository<TEntity> where TEntity : class
    {
        IQueryable<TEntity> Query(Expression<Func<TEntity, bool>> filter = null);


        IQueryable<TEntity> QueryWithNoTracking();

        void Add(TEntity item);

        void Update(TEntity item);

        void AddRange(IEnumerable<TEntity> items);
        void UpdateRange(IEnumerable<TEntity> items);

        void Remove(TEntity item);

        void RemoveRange(IEnumerable<TEntity> items);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // Query
        Task<T?> GetByIdAsync(object id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        // Query with Navigation Properties
        Task<IReadOnlyList<T>> GetAllWithIncludesAsync(params Expression<Func<T, object>>[] includes);
        Task<T?> GetByIdWithIncludesAsync(object id, params Expression<Func<T, object>>[] includes);

        // Pagination
        Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false);

        // Write
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

        // Soft Delete
        Task SoftDeleteAsync(object id, string deletedByUserId);
        Task RestoreAsync(object id);
        Task<IReadOnlyList<T>> GetDeletedAsync();

        // Raw Queryable
        IQueryable<T> AsQueryable();
        IQueryable<T> AsQueryableIgnoringSoftDelete();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExhibitionManagementSystem.DataAccess.Repositories.Interfaces;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.DataAccess.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate != null 
                ? await _dbSet.CountAsync(predicate) 
                : await _dbSet.CountAsync();
        }

        public async Task<IReadOnlyList<T>> GetAllWithIncludesAsync(params Expression<Func<T, object>>[] includes)
        {
            var query = _dbSet.AsQueryable();
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<T?> GetByIdWithIncludesAsync(object id, params Expression<Func<T, object>>[] includes)
        {
            var query = _dbSet.AsQueryable();
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            var keyName = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
            if (keyName == null) 
                return null;

            var parameter = Expression.Parameter(typeof(T), "e");
            var property = Expression.Property(parameter, keyName);
            var convertedId = Expression.Constant(Convert.ChangeType(id, property.Type));
            var body = Expression.Equal(property, convertedId);
            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

            return await query.FirstOrDefaultAsync(lambda);
        }

        public async Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync();

            if (orderBy != null)
            {
                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public async Task SoftDeleteAsync(object id, string deletedByUserId)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) 
                return;

            if (entity is ISoftDeletable softDeletable)
            {
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = DateTime.UtcNow;
                softDeletable.DeletedByUserId = deletedByUserId;
                _context.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                throw new NotSupportedException($"{typeof(T).Name} does not support soft delete.");
            }
        }

        public async Task RestoreAsync(object id)
        {
            var keyName = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
            if (keyName == null) 
                throw new InvalidOperationException("Entity does not have a primary key.");

            var parameter = Expression.Parameter(typeof(T), "e");
            var property = Expression.Property(parameter, keyName);
            var convertedId = Expression.Constant(Convert.ChangeType(id, property.Type));
            var body = Expression.Equal(property, convertedId);
            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

            var entity = await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(lambda);
            if (entity == null) 
                return;

            if (entity is ISoftDeletable softDeletable)
            {
                softDeletable.IsDeleted = false;
                softDeletable.DeletedAt = null;
                softDeletable.DeletedByUserId = null;
                _context.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                throw new NotSupportedException($"{typeof(T).Name} does not support soft delete.");
            }
        }

        public async Task<IReadOnlyList<T>> GetDeletedAsync()
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
            {
                return await _dbSet.IgnoreQueryFilters()
                    .Where(e => ((ISoftDeletable)e).IsDeleted)
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                throw new NotSupportedException($"{typeof(T).Name} does not support soft delete.");
            }
        }

        public IQueryable<T> AsQueryable()
        {
            return _dbSet.AsQueryable();
        }

        public IQueryable<T> AsQueryableIgnoringSoftDelete()
        {
            return _dbSet.IgnoreQueryFilters().AsQueryable();
        }
    }
}

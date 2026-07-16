using System.Linq.Expressions;
using AIEventDiscovery.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIEventDiscovery.Data.Repositories;

/// <summary>
/// Generic repository implementation for PostgreSQL database operations.
/// Handles all standard CRUD, soft-delete, bulk, and transaction operations.
/// Inherit this class to create entity-specific repositories with extra queries.
/// </summary>
/// <typeparam name="TEntity">Must inherit <see cref="BaseEntity"/>.</typeparam>
public class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet   = context.Set<TEntity>();
    }

    /// <inheritdoc/>
    public IQueryable<TEntity> GetAll(
        Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[]? includes)
    {
        IQueryable<TEntity> query = _dbSet
            .AsNoTracking()
            .Where(e => !e.IsDeleted);

        if (includes != null)
        {
            foreach (var include in includes)
                query = query.Include(include);
        }

        if (predicate != null)
            query = query.Where(predicate);

        return query;
    }

    /// <inheritdoc/>
    public async Task<TResult?> GetByIdAsync<TResult>(
        Guid id,
        Expression<Func<TEntity, TResult>>? selector = null,
        params Expression<Func<TEntity, object>>[]? includes)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (includes != null)
        {
            foreach (var include in includes)
                query = query.Include(include);
        }

        query = query.Where(e => e.Id == id && !e.IsDeleted);

        if (selector != null)
            return await query.Select(selector).FirstOrDefaultAsync();

        return await query.Cast<TResult>().FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = _dbSet.AsNoTracking().Where(e => !e.IsDeleted);

        if (includes != null)
        {
            foreach (var include in includes)
                query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(predicate);
    }

    /// <inheritdoc/>
    public async Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool useAsNoTracking)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var query = _dbSet.Where(e => !e.IsDeleted);

        if (useAsNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(predicate);
    }

    /// <inheritdoc/>
    public async Task<bool> IsExistsAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .AnyAsync(predicate);
    }

    /// <inheritdoc/>
    public async Task<TEntity> UpsertAsync(TEntity entity, bool shouldUseTransaction = false)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var existing = await _dbSet.FirstOrDefaultAsync(e => e.Id == entity.Id);

        if (existing == null)
        {
            _dbSet.Add(entity);
        }
        else
        {
            entity.CreatedAt = existing.CreatedAt;
            entity.UpdatedAt = DateTime.UtcNow;
            _context.Entry(existing).CurrentValues.SetValues(entity);
        }

        if (!shouldUseTransaction)
            await _context.SaveChangesAsync();

        return entity;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, bool hardDelete = false, bool shouldUseTransaction = false)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id);

        if (entity == null)
            return false;

        if (hardDelete)
        {
            _dbSet.Remove(entity);
        }
        else
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            _context.Entry(entity).State = EntityState.Modified;
        }

        if (!shouldUseTransaction)
            await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> BulkInsertAsync(IEnumerable<TEntity> entities, bool shouldUseTransaction = false)
    {
        ArgumentNullException.ThrowIfNull(entities);

        await _context.AddRangeAsync(entities);

        if (!shouldUseTransaction)
            await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> BulkUpdateAsync(IEnumerable<TEntity> entities, bool shouldUseTransaction = false)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var utcNow = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.UpdatedAt = utcNow;
            _context.Update(entity);
        }

        if (!shouldUseTransaction)
            await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> BulkRemoveAsync(
        IEnumerable<Guid> ids,
        Guid userId,
        bool shouldUseTransaction = false,
        bool hardDelete = false)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var entities = await _context.Set<TEntity>()
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();

        if (!entities.Any())
            return false;

        var utcNow = DateTime.UtcNow;

        if (hardDelete)
        {
            _context.RemoveRange(entities);
        }
        else
        {
            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = utcNow;
                entity.UpdatedBy = userId;
                _context.Update(entity);
            }
        }

        if (!shouldUseTransaction)
            await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ExecuteTransactionAsync(Func<Task> operation)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                var affected = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return affected > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw; 
            }
        });
    }
}

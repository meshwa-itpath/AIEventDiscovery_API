using System.Linq.Expressions;
using AIEventDiscovery.Entities;

namespace AIEventDiscovery.Data.Repositories;

/// <summary>
/// Generic repository contract for all PostgreSQL entity operations.
/// All CRUD, bulk, and transaction operations go through this interface.
/// </summary>
public interface IGenericRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>Returns a queryable of non-deleted entities, with optional filtering and includes.</summary>
    IQueryable<TEntity> GetAll(
        Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[]? includes);

    /// <summary>Gets an entity by ID, optionally projecting into a result type.</summary>
    Task<TResult?> GetByIdAsync<TResult>(
        Guid id,
        Expression<Func<TEntity, TResult>>? selector = null,
        params Expression<Func<TEntity, object>>[]? includes);

    /// <summary>Finds the first non-deleted entity matching the predicate (AsNoTracking).</summary>
    Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes);

    /// <summary>Finds the first non-deleted entity matching the predicate with tracking control.</summary>
    Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool useAsNoTracking);

    /// <summary>Inserts a new entity or updates an existing one based on Id.</summary>
    Task<TEntity> UpsertAsync(TEntity entity, bool shouldUseTransaction = false);

    /// <summary>Soft-deletes (default) or hard-deletes an entity by Id.</summary>
    Task<bool> DeleteAsync(Guid id, bool hardDelete = false, bool shouldUseTransaction = false);

    /// <summary>Returns true if any non-deleted entity matches the predicate.</summary>
    Task<bool> IsExistsAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>Executes multiple operations inside a single database transaction.</summary>
    Task<bool> ExecuteTransactionAsync(Func<Task> operation);

    /// <summary>Inserts a batch of entities in a single round-trip.</summary>
    Task<bool> BulkInsertAsync(IEnumerable<TEntity> entities, bool shouldUseTransaction = false);

    /// <summary>Updates a batch of entities.</summary>
    Task<bool> BulkUpdateAsync(IEnumerable<TEntity> entities, bool shouldUseTransaction = false);

    /// <summary>Soft-deletes or hard-deletes a batch of entities by their Ids.</summary>
    Task<bool> BulkRemoveAsync(IEnumerable<Guid> ids, Guid userId, bool shouldUseTransaction = false, bool hardDelete = false);
}

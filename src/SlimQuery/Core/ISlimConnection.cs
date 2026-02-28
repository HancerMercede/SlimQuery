using System.Data;
using SlimQuery.Query;

namespace SlimQuery.Core;

public interface ISlimConnection : IAsyncDisposable
{
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? tx = null);
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? tx = null);
    Task<T> ExecuteScalarAsync<T>(string sql, object? param = null, IDbTransaction? tx = null);
    Task<int> ExecuteAsync(string sql, object? param = null, IDbTransaction? tx = null);

    Task<T?> GetByIdAsync<T>(object id, IDbTransaction? tx = null);
    Task<long> InsertAsync<T>(T entity, IDbTransaction? tx = null);
    Task UpdateAsync<T>(T entity, IDbTransaction? tx = null);
    Task DeleteAsync<T>(object id, IDbTransaction? tx = null);

    IQueryBuilder<T> From<T>();

    Task BulkInsertAsync<T>(IEnumerable<T> entities, IDbTransaction? tx = null);
    Task BulkUpdateAsync<T>(IEnumerable<T> entities, IDbTransaction? tx = null);
    Task BulkDeleteAsync<T>(IEnumerable<object> ids, IDbTransaction? tx = null);

    Task<SlimTransaction> BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted);

    void InvalidateCache(string tag);
}

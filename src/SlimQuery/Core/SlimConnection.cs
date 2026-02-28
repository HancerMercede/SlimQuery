using System.Data;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using SlimQuery.Mapping;
using SlimQuery.Query;
using SlimQuery.Query.SqlDialect;

namespace SlimQuery.Core;

public class SlimConnection : ISlimConnection
{
    private readonly IDbConnection _connection;
    private readonly ISqlDialect _dialect;
    private readonly IMemoryCache? _cache;

    public SlimConnection(IDbConnection connection, ISqlDialect? dialect = null, IMemoryCache? cache = null)
    {
        _connection = connection;
        _dialect = dialect ?? new SqliteDialect();
        _cache = cache;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, IDbTransaction? tx = null)
    {
        EnsureConnection();
        return await _connection.QueryAsync<T>(sql, param, tx);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, IDbTransaction? tx = null)
    {
        EnsureConnection();
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, param, tx);
    }

    public async Task<T> ExecuteScalarAsync<T>(string sql, object? param = null, IDbTransaction? tx = null)
    {
        EnsureConnection();
        var result = await _connection.ExecuteScalarAsync<T>(sql, param, tx);
        return result!;
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, IDbTransaction? tx = null)
    {
        EnsureConnection();
        return await _connection.ExecuteAsync(sql, param, tx);
    }

    public async Task<T?> GetByIdAsync<T>(object id, IDbTransaction? tx = null)
    {
        var tableName = GetTableName<T>();
        var pkName = GetPrimaryKeyName<T>();
        var sql = $"SELECT * FROM {tableName} WHERE {pkName} = @Id";
        return await QueryFirstOrDefaultAsync<T>(sql, new { Id = id }, tx);
    }

    public async Task<long> InsertAsync<T>(T entity, IDbTransaction? tx = null)
    {
        var tableName = GetTableName<T>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.Name != "Id")
            .ToList();

        var columns = string.Join(", ", properties.Select(p => _dialect.EscapeIdentifier(GetColumnName(p))));
        var parameters = string.Join(", ", properties.Select(p => $"@{p.Name}"));

        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}); SELECT last_insert_rowid();";

        var id = await ExecuteScalarAsync<long>(sql, entity, tx);
        return id;
    }

    public async Task UpdateAsync<T>(T entity, IDbTransaction? tx = null)
    {
        var tableName = GetTableName<T>();
        var pkName = GetPrimaryKeyName<T>();
        var pkValue = typeof(T).GetProperty("Id")?.GetValue(entity);

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.Name != "Id")
            .ToList();

        var setClause = string.Join(", ", properties.Select(p => $"{_dialect.EscapeIdentifier(GetColumnName(p))} = @{p.Name}"));
        var sql = $"UPDATE {tableName} SET {setClause} WHERE {pkName} = @Id";

        await ExecuteAsync(sql, entity, tx);
    }

    public async Task DeleteAsync<T>(object id, IDbTransaction? tx = null)
    {
        var tableName = GetTableName<T>();
        var pkName = GetPrimaryKeyName<T>();
        var sql = $"DELETE FROM {tableName} WHERE {pkName} = @Id";
        await ExecuteAsync(sql, new { Id = id }, tx);
    }

    public IQueryBuilder<T> From<T>()
    {
        return new QueryBuilder<T>(this, _dialect);
    }

    public async Task BulkInsertAsync<T>(IEnumerable<T> entities, IDbTransaction? tx = null)
    {
        var tableName = GetTableName<T>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        var columns = string.Join(", ", properties.Select(p => _dialect.EscapeIdentifier(GetColumnName(p))));
        
        foreach (var entity in entities)
        {
            var parameters = string.Join(", ", properties.Select(p => $"@{p.Name}"));
            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";
            await _connection.ExecuteAsync(sql, entity, tx);
        }
    }

    public async Task BulkUpdateAsync<T>(IEnumerable<T> entities, IDbTransaction? tx = null)
    {
        foreach (var entity in entities)
        {
            await UpdateAsync(entity, tx);
        }
    }

    public async Task BulkDeleteAsync<T>(IEnumerable<object> ids, IDbTransaction? tx = null)
    {
        foreach (var id in ids)
        {
            await DeleteAsync<T>(id, tx);
        }
    }

    public Task<SlimTransaction> BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted)
    {
        EnsureConnection();
        var tx = _connection.BeginTransaction(level);
        return Task.FromResult(new SlimTransaction(tx));
    }

    public void InvalidateCache(string tag)
    {
        _cache?.Remove(tag);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection.State != ConnectionState.Closed)
        {
            _connection.Close();
        }
        _connection.Dispose();
    }

    private void EnsureConnection()
    {
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }
    }

    private static string GetTableName<T>()
    {
        var type = typeof(T);
        var name = type.Name;
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append('_');
            sb.Append(char.ToLower(c));
        }
        return sb.ToString() + "s";
    }

    private static string GetColumnName(PropertyInfo prop)
    {
        return prop.Name;
    }

    private static string GetPrimaryKeyName<T>()
    {
        return "id";
    }
}

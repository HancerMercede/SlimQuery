using System.Data;
using System.Reflection;
using System.Text;
using Dapper;
using SlimQuery.Query.SqlDialect;

namespace SlimQuery.Bulk;

public class BulkOperations
{
    private readonly SlimQuery.Core.SlimConnection _connection;
    private readonly ISqlDialect _dialect;

    public BulkOperations(SlimQuery.Core.SlimConnection connection, ISqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    public async Task BulkInsertAsync<T>(IEnumerable<T> entities, IDbTransaction? tx = null)
    {
        var entityList = entities.ToList();
        if (!entityList.Any()) return;

        var tableName = GetTableName<T>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        var columns = string.Join(", ", properties.Select(p => _dialect.EscapeIdentifier(GetColumnName(p))));

        foreach (var entity in entityList)
        {
            var parameters = string.Join(", ", properties.Select(p => $"@{p.Name}"));
            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";
            await _connection.ExecuteAsync(sql, entity, tx);
        }
    }

    public async Task BulkInsertBatchedAsync<T>(IEnumerable<T> entities, IDbTransaction? tx = null, int batchSize = 1000)
    {
        var entityList = entities.ToList();
        if (!entityList.Any()) return;

        var tableName = GetTableName<T>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToList();

        var columns = string.Join(", ", properties.Select(p => _dialect.EscapeIdentifier(GetColumnName(p))));

        var batches = entityList
            .Select((entity, index) => new { entity, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.entity).ToList());

        foreach (var batch in batches)
        {
            var sql = new StringBuilder();
            foreach (var entity in batch)
            {
                var parameters = string.Join(", ", properties.Select(p => $"@{p.Name}"));
                sql.AppendLine($"INSERT INTO {tableName} ({columns}) VALUES ({parameters});");
            }

            await _connection.ExecuteAsync(sql.ToString(), null, tx);
        }
    }

    public async Task BulkUpdateAsync<T>(IEnumerable<T> entities, IDbTransaction? tx = null)
    {
        var entityList = entities.ToList();
        if (!entityList.Any()) return;

        foreach (var entity in entityList)
        {
            await _connection.UpdateAsync(entity, tx);
        }
    }

    public async Task BulkDeleteAsync<T>(IEnumerable<object> ids, IDbTransaction? tx = null)
    {
        var idList = ids.ToList();
        if (!idList.Any()) return;

        foreach (var id in idList)
        {
            await _connection.DeleteAsync<T>(id, tx);
        }
    }

    public async Task BulkDeleteByIdsAsync<T>(IEnumerable<object> ids, IDbTransaction? tx = null)
    {
        var idList = ids.ToList();
        if (!idList.Any()) return;

        var tableName = GetTableName<T>();
        var pkName = GetPrimaryKeyName<T>();

        var idsParam = string.Join(",", idList.Select((_, i) => $"@Id{i}"));
        var sql = $"DELETE FROM {tableName} WHERE {pkName} IN ({idsParam})";

        var parameters = new DynamicParameters();
        for (int i = 0; i < idList.Count; i++)
        {
            parameters.Add($"Id{i}", idList[i]);
        }

        await _connection.ExecuteAsync(sql, parameters, tx);
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

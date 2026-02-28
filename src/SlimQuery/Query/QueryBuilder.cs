using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using SlimQuery.Core;
using SlimQuery.Query.SqlDialect;
using SlimQuery.Relations;

namespace SlimQuery.Query;

public class QueryBuilder<T> : IQueryBuilder<T>
{
    private readonly SlimConnection _connection;
    private readonly ISqlDialect _dialect;
    private readonly QueryContext<T> _context;

    public QueryBuilder(SlimConnection connection, ISqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
        _context = new QueryContext<T>
        {
            TableName = GetTableName()
        };
    }

    public IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        var sql = ExpressionExtractor.ToSql(predicate, _dialect);
        _context.WhereClauses.Add(sql);
        return this;
    }

    public IQueryBuilder<T> OrderBy(Expression<Func<T, object>> selector)
    {
        var columns = ExpressionExtractor.GetMemberNames(selector);
        foreach (var col in columns)
        {
            _context.OrderByClauses.Add($"{_dialect.EscapeIdentifier(col)} ASC");
        }
        return this;
    }

    public IQueryBuilder<T> OrderByDesc(Expression<Func<T, object>> selector)
    {
        var columns = ExpressionExtractor.GetMemberNames(selector);
        foreach (var col in columns)
        {
            _context.OrderByClauses.Add($"{_dialect.EscapeIdentifier(col)} DESC");
        }
        return this;
    }

    public IQueryBuilder<T> Skip(int count)
    {
        _context.Skip = count;
        return this;
    }

    public IQueryBuilder<T> Take(int count)
    {
        _context.Take = count;
        return this;
    }

    public IQueryBuilder<T> Select(Expression<Func<T, object>> selector)
    {
        var columns = ExpressionExtractor.GetMemberNames(selector);
        _context.SelectColumns.AddRange(columns);
        return this;
    }

    public IQueryBuilder<T> Include<TRelated>(Expression<Func<T, object>> selector)
    {
        _context.Includes.Add((typeof(TRelated), selector));
        return this;
    }

    public IQueryBuilder<T> Cache(TimeSpan ttl, string? tag = null)
    {
        _context.CacheTtl = ttl;
        _context.CacheTag = tag;
        return this;
    }

    public async Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var sql = BuildSelect();
        var results = await _connection.QueryAsync<T>(sql, _context.Parameters);
        var list = results.ToList();

        if (_context.Includes.Any() && list.Any())
        {
            var loader = new RelationLoader(_connection, _dialect);
            await loader.LoadRelations(list);
        }

        return list;
    }

    public async Task<T?> FirstAsync(CancellationToken cancellationToken = default)
    {
        var sql = BuildSelect() + " LIMIT 1";
        var result = await _connection.QueryFirstOrDefaultAsync<T>(sql, _context.Parameters);
        
        if (result != null && _context.Includes.Any())
        {
            var loader = new RelationLoader(_connection, _dialect);
            await loader.LoadRelations(new[] { result });
        }

        return result;
    }

    public async Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await FirstAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var tableName = _context.TableName;
        var sql = $"SELECT COUNT(*) FROM {_dialect.EscapeIdentifier(tableName)}";
        
        if (_context.WhereClauses.Any())
        {
            sql += " WHERE " + string.Join(" AND ", _context.WhereClauses);
        }
        
        return await _connection.ExecuteScalarAsync<int>(sql, _context.Parameters);
    }

    private string BuildSelect()
    {
        var tableName = _context.TableName;
        var columns = _context.SelectColumns.Any() 
            ? string.Join(", ", _context.SelectColumns.Select(c => _dialect.EscapeIdentifier(c)))
            : "*";
        
        var sql = new StringBuilder($"SELECT {columns} FROM {_dialect.EscapeIdentifier(tableName)}");

        if (_context.WhereClauses.Any())
        {
            sql.Append(" WHERE " + string.Join(" AND ", _context.WhereClauses));
        }

        if (_context.OrderByClauses.Any())
        {
            sql.Append(" ORDER BY " + string.Join(", ", _context.OrderByClauses));
        }

        if (_context.Skip > 0 || _context.Take > 0)
        {
            sql.Append($" LIMIT {_context.Skip}, {_context.Take}");
        }

        return sql.ToString();
    }

    private string GetTableName()
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
}

using System.Linq.Expressions;

namespace SlimQuery.Query;

public interface IQueryBuilder<T>
{
    IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate);
    IQueryBuilder<T> OrderBy(Expression<Func<T, object>> selector);
    IQueryBuilder<T> OrderByDesc(Expression<Func<T, object>> selector);
    IQueryBuilder<T> Skip(int count);
    IQueryBuilder<T> Take(int count);
    IQueryBuilder<T> Select(Expression<Func<T, object>> selector);
    IQueryBuilder<T> Include<TRelated>(Expression<Func<T, object>> selector);
    IQueryBuilder<T> Cache(TimeSpan ttl, string? tag = null);

    Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default);
    Task<T?> FirstAsync(CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

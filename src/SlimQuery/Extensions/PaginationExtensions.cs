using System.Linq;
using SlimQuery.Query;

namespace SlimQuery.Extensions;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public static class PaginationExtensions
{
    public static async Task<PagedResult<T>> PaginateAsync<T>(
        this IQueryBuilder<T> queryBuilder,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var totalCount = await queryBuilder.CountAsync(cancellationToken);
        
        var items = await queryBuilder
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public static PagedResult<T> ToPagedList<T>(
        this IEnumerable<T> source,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var totalCount = source.Count();
        var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public static PagedResult<T> ToPagedList<T>(
        this IQueryable<T> source,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var totalCount = source.Count();
        var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

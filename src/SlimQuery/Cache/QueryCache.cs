using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace SlimQuery.Cache;

public class QueryCache
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, List<string>> _tagIndex = new();

    public QueryCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key)
    {
        return _cache.Get<T>(key);
    }

    public void Set<T>(string key, T value, TimeSpan? ttl = null, string? tag = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (ttl.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = ttl.Value;
        }

        if (!string.IsNullOrEmpty(tag))
        {
            if (!_tagIndex.ContainsKey(tag))
            {
                _tagIndex[tag] = new List<string>();
            }
            _tagIndex[tag].Add(key);
        }

        _cache.Set(key, value, options);
    }

    public void InvalidateTag(string tag)
    {
        if (_tagIndex.TryRemove(tag, out var keys))
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
        }
    }

    public void InvalidateAll()
    {
        _tagIndex.Clear();
    }
}

public static class CacheKeys
{
    public static string Query<T>(string sql, object? param = null)
    {
        var paramHash = param?.GetHashCode() ?? 0;
        return $"query:{typeof(T).Name}:{sql.GetHashCode()}:{paramHash}";
    }

    public static string Entity<T>(object id)
    {
        return $"entity:{typeof(T).Name}:{id}";
    }
}

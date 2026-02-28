using System.Collections.Concurrent;
using System.Data;

namespace SlimQuery.Mapping;

public static class MapperCache
{
    private static readonly ConcurrentDictionary<Type, object> _mappers = new();

    public static Func<IDataReader, T> GetOrAdd<T>(Func<Type, Func<IDataReader, object>> factory)
    {
        var type = typeof(T);
        return (Func<IDataReader, T>)_mappers.GetOrAdd(type, t => factory(t));
    }

    public static void Clear()
    {
        _mappers.Clear();
    }
}

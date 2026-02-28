using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace SlimQuery.Mapping;

public static class ObjectMapper
{
    private static readonly ConcurrentDictionary<Type, object> _mappers = new();

    public static T Map<T>(IDataReader reader)
    {
        var mapper = GetMapper<T>();
        return mapper(reader);
    }

    public static IEnumerable<T> MapMultiple<T>(IDataReader reader)
    {
        var mapper = GetMapper<T>();
        var list = new List<T>();
        while (reader.Read())
        {
            list.Add(mapper(reader));
        }
        return list;
    }

    private static Func<IDataReader, T> GetMapper<T>()
    {
        var type = typeof(T);
        if (_mappers.TryGetValue(type, out var cached))
        {
            return (Func<IDataReader, T>)cached;
        }

        var mapper = CreateMapper<T>();
        _mappers[type] = mapper;
        return mapper!;
    }

    private static Func<IDataReader, T> CreateMapper<T>()
    {
        var type = typeof(T);
        var readerParam = Expression.Parameter(typeof(IDataReader), "reader");
        var bindings = new List<MemberBinding>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanWrite) continue;

            var getOrdinal = CreateGetOrdinalExpression(readerParam, prop.Name);
            var getValue = Expression.Convert(
                Expression.Call(readerParam, nameof(IDataReader.GetValue), Type.EmptyTypes, getOrdinal),
                prop.PropertyType
            );
            
            bindings.Add(Expression.Bind(prop, getValue));
        }

        var memberInit = Expression.MemberInit(Expression.New(type), bindings);
        var lambda = Expression.Lambda<Func<IDataReader, T>>(memberInit, readerParam);
        return lambda.Compile();
    }

    private static Expression CreateGetOrdinalExpression(Expression readerParam, string columnName)
    {
        var getOrdinalMethod = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetOrdinal))!;
        var ordinalCall = Expression.Call(readerParam, getOrdinalMethod, Expression.Constant(columnName));
        return Expression.Convert(ordinalCall, typeof(int));
    }

    public static void ClearCache()
    {
        _mappers.Clear();
    }
}

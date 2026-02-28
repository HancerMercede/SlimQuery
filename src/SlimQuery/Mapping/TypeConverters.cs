using System.Data;
using System.Globalization;

namespace SlimQuery.Mapping;

public static class TypeConverters
{
    private static readonly Dictionary<Type, Func<object, object>> _converters = new()
    {
        { typeof(DateTime), ConvertDateTime },
        { typeof(Guid), ConvertGuid },
        { typeof(bool), ConvertBool },
        { typeof(decimal), ConvertDecimal },
    };

    public static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null || value == DBNull.Value)
            return null;

        var sourceType = value.GetType();
        if (sourceType == targetType || targetType.IsAssignableFrom(sourceType))
            return value;

        if (_converters.TryGetValue(targetType, out var converter))
            return converter(value);

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static object ConvertDateTime(object value)
    {
        if (value is DateTime dt) return dt;
        return DateTime.Parse(value.ToString()!, CultureInfo.InvariantCulture);
    }

    private static object ConvertGuid(object value)
    {
        if (value is Guid g) return g;
        return Guid.Parse(value.ToString()!);
    }

    private static object ConvertBool(object value)
    {
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        return bool.Parse(value.ToString()!);
    }

    private static object ConvertDecimal(object value)
    {
        if (value is decimal d) return d;
        return decimal.Parse(value.ToString()!, CultureInfo.InvariantCulture);
    }
}

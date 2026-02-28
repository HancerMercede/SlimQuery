using System.Linq.Expressions;
using System.Reflection;

namespace SlimQuery.Relations;

public enum RelationType
{
    HasOne,
    HasMany
}

public class RelationConfig
{
    public string ParentTable { get; set; } = string.Empty;
    public string ChildTable { get; set; } = string.Empty;
    public string ForeignKey { get; set; } = string.Empty;
    public string PrimaryKey { get; set; } = string.Empty;
    public RelationType Type { get; set; }
}

public static class RelationConfigCache
{
    private static readonly Dictionary<Type, List<RelationConfig>> _configs = new();

    public static void Add<TParent>(RelationConfig config)
    {
        var type = typeof(TParent);
        if (!_configs.ContainsKey(type))
        {
            _configs[type] = new List<RelationConfig>();
        }
        _configs[type].Add(config);
    }

    public static List<RelationConfig> Get<T>()
    {
        return _configs.TryGetValue(typeof(T), out var config) ? config : new List<RelationConfig>();
    }

    public static RelationConfig? Get<TParent, TChild>()
    {
        if (_configs.TryGetValue(typeof(TParent), out var configs))
        {
            return configs.FirstOrDefault(c => c.ChildTable == typeof(TChild).Name.ToLower() + "s");
        }
        return null;
    }

    public static void Clear() => _configs.Clear();
}

public static class RelationBuilder
{
    public static HasOneBuilder<T> HasOne<T>(Expression<Func<T, object>> reference)
    {
        var propName = GetMemberName(reference);
        return new HasOneBuilder<T>(propName);
    }

    public static HasManyBuilder<T> HasMany<T>(Expression<Func<T, object>> collection)
    {
        var propName = GetMemberName(collection);
        return new HasManyBuilder<T>(propName);
    }

    private static string GetMemberName(LambdaExpression expr)
    {
        if (expr.Body is MemberExpression member)
            return member.Member.Name;
        if (expr.Body is UnaryExpression unary && unary.Operand is MemberExpression m)
            return m.Member.Name;
        throw new ArgumentException("Expression must be a member access");
    }
}

public class HasOneBuilder<T>
{
    private readonly string _propertyName;

    public HasOneBuilder(string propertyName)
    {
        _propertyName = propertyName;
    }

    public void WithForeignKey(string fk)
    {
        var childType = typeof(T).GetProperty(_propertyName)?.PropertyType;
        if (childType == null) return;

        var parentTable = typeof(T).Name.ToLower() + "s";
        var childTable = childType.Name.ToLower() + "s";

        RelationConfigCache.Add<T>(new RelationConfig
        {
            ParentTable = parentTable,
            ChildTable = childTable,
            ForeignKey = fk,
            PrimaryKey = "id",
            Type = RelationType.HasOne
        });
    }
}

public class HasManyBuilder<T>
{
    private readonly string _propertyName;

    public HasManyBuilder(string propertyName)
    {
        _propertyName = propertyName;
    }

    public void WithForeignKey(string fk)
    {
        var elementType = typeof(T).GetProperty(_propertyName)?.PropertyType;
        if (elementType == null) return;

        var parentTable = typeof(T).Name.ToLower() + "s";
        var childTable = elementType.Name.ToLower() + "s";

        RelationConfigCache.Add<T>(new RelationConfig
        {
            ParentTable = parentTable,
            ChildTable = childTable,
            ForeignKey = fk,
            PrimaryKey = "id",
            Type = RelationType.HasMany
        });
    }
}

using System.Linq.Expressions;
using System.Reflection;
using SlimQuery.Query.SqlDialect;

namespace SlimQuery.Query;

public class QueryContext<T>
{
    public string TableName { get; set; } = string.Empty;
    public List<string> WhereClauses { get; set; } = new();
    public List<string> OrderByClauses { get; set; } = new();
    public List<string> SelectColumns { get; set; } = new();
    public List<(Type RelatedType, LambdaExpression Selector)> Includes { get; set; } = new();
    public int Skip { get; set; }
    public int Take { get; set; }
    public object? Parameters { get; set; }
    public TimeSpan? CacheTtl { get; set; }
    public string? CacheTag { get; set; }
}

public static class ExpressionExtractor
{
    public static string ToSql(LambdaExpression expr, ISqlDialect dialect)
    {
        return Visit(expr.Body, dialect);
    }

    private static string Visit(Expression expr, ISqlDialect dialect)
    {
        return expr switch
        {
            BinaryExpression binary => VisitBinary(binary, dialect),
            MemberExpression member => VisitMember(member),
            UnaryExpression unary => VisitUnary(unary, dialect),
            ConstantExpression constant => VisitConstant(constant),
            MethodCallExpression method => VisitMethod(method, dialect),
            NewExpression newExpr => VisitNew(newExpr),
            _ => throw new NotSupportedException($"Expression type {expr.GetType()} not supported")
        };
    }

    private static string VisitBinary(BinaryExpression expr, ISqlDialect dialect)
    {
        var left = Visit(expr.Left, dialect);
        var right = Visit(expr.Right, dialect);
        var op = expr.NodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            _ => throw new NotSupportedException($"Operator {expr.NodeType} not supported")
        };
        return $"({left} {op} {right})";
    }

    private static string VisitMember(MemberExpression expr)
    {
        if (expr.Expression is ConstantExpression constant)
        {
            var value = Expression.Lambda(expr).Compile().DynamicInvoke();
            return FormatValue(value);
        }
        return GetColumnName(expr);
    }

    private static string VisitUnary(UnaryExpression expr, ISqlDialect dialect)
    {
        if (expr.NodeType == ExpressionType.Not)
        {
            return $"NOT {Visit(expr.Operand, dialect)}";
        }
        return Visit(expr.Operand, dialect);
    }

    private static string VisitConstant(ConstantExpression expr)
    {
        return FormatValue(expr.Value);
    }

    private static string VisitMethod(MethodCallExpression expr, ISqlDialect dialect)
    {
        var obj = expr.Object != null ? Visit(expr.Object, dialect) : null;
        var methodName = expr.Method.Name;

        return methodName switch
        {
            "Contains" when expr.Arguments.Count == 1 => 
                $"{obj} IN ({VisitList(expr.Arguments[0], dialect)})",
            "Equals" => $"({obj} = {Visit(expr.Arguments[0], dialect)})",
            "StartsWith" => $"({obj} LIKE ({Visit(expr.Arguments[0], dialect)} || '%'))",
            "EndsWith" => $"({obj} LIKE ('%' || {Visit(expr.Arguments[0], dialect)}))",
            "Contains" when expr.Arguments.Count == 2 =>
                $"({obj} LIKE ('%' || {Visit(expr.Arguments[0], dialect)} || '%'))",
            _ => throw new NotSupportedException($"Method {methodName} not supported")
        };
    }

    private static string VisitNew(NewExpression expr)
    {
        return string.Join(", ", expr.Members?.Select(m => m.Name) ?? Enumerable.Empty<string>());
    }

    private static string VisitList(Expression expr, ISqlDialect dialect)
    {
        if (expr is NewArrayExpression arr)
        {
            return string.Join(", ", arr.Expressions.Select(e => Visit(e, dialect)));
        }
        if (expr is ConstantExpression constant && constant.Value is IEnumerable<object> list)
        {
            return string.Join(", ", list.Select(FormatValue));
        }
        throw new NotSupportedException("List expression not supported");
    }

    private static string GetColumnName(MemberExpression expr)
    {
        var name = expr.Member.Name;
        if (expr.Expression is MemberExpression parent)
        {
            return GetColumnName(parent) + "." + name;
        }
        return name;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s => $"'{s.Replace("'", "''")}'",
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            Guid g => $"'{g}'",
            _ => value.ToString() ?? "NULL"
        };
    }

    public static string GetMemberName(LambdaExpression expr)
    {
        if (expr.Body is MemberExpression member)
            return member.Member.Name;
        if (expr.Body is UnaryExpression unary && unary.Operand is MemberExpression m)
            return m.Member.Name;
        throw new ArgumentException("Expression must be a member access");
    }

    public static IEnumerable<string> GetMemberNames(LambdaExpression expr)
    {
        if (expr.Body is NewExpression newExpr)
        {
            return newExpr.Members?.Select(m => m.Name) ?? Enumerable.Empty<string>();
        }
        return new[] { GetMemberName(expr) };
    }
}

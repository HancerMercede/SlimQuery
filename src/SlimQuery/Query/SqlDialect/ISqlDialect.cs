namespace SlimQuery.Query.SqlDialect;

public interface ISqlDialect
{
    string EscapeIdentifier(string name);
    string GetParameterName(string name);
    string GetSelectTop(int count);
    string GetLimit(int offset, int count);
    string Quote(string value);
}

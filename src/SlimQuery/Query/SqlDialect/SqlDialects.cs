namespace SlimQuery.Query.SqlDialect;

public class SqliteDialect : ISqlDialect
{
    public string EscapeIdentifier(string name) => $"\"{name}\"";
    public string GetParameterName(string name) => $"@{name}";
    public string GetSelectTop(int count) => $"LIMIT {count}";
    public string GetLimit(int offset, int count) => $"LIMIT {offset}, {count}";
    public string Quote(string value) => $"'{value.Replace("'", "''")}'";
}

public class PostgresDialect : ISqlDialect
{
    public string EscapeIdentifier(string name) => $"\"{name}\"";
    public string GetParameterName(string name) => $"@{name}";
    public string GetSelectTop(int count) => $"LIMIT {count}";
    public string GetLimit(int offset, int count) => $"LIMIT {count} OFFSET {offset}";
    public string Quote(string value) => $"'{value.Replace("'", "''")}'";
}

public class SqlServerDialect : ISqlDialect
{
    public string EscapeIdentifier(string name) => $"[{name}]";
    public string GetParameterName(string name) => $"@{name}";
    public string GetSelectTop(int count) => $"TOP {count}";
    public string GetLimit(int offset, int count) => $"OFFSET {offset} ROWS FETCH NEXT {count} ROWS ONLY";
    public string Quote(string value) => $"'{value.Replace("'", "''")}'";
}

public class MySqlDialect : ISqlDialect
{
    public string EscapeIdentifier(string name) => $"`{name}`";
    public string GetParameterName(string name) => $"@{name}";
    public string GetSelectTop(int count) => $"LIMIT {count}";
    public string GetLimit(int offset, int count) => $"LIMIT {offset}, {count}";
    public string Quote(string value) => $"'{value.Replace("'", "''")}'";
}

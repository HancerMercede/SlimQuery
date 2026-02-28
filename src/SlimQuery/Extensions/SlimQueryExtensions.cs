using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SlimQuery.Cache;
using SlimQuery.Core;
using SlimQuery.Query.SqlDialect;

namespace SlimQuery.Extensions;

public static class SlimQueryExtensions
{
    public static IServiceCollection AddSlimQuery(
        this IServiceCollection services,
        Func<IDbConnection> connectionFactory,
        Action<SlimQueryOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure<SlimQueryOptions>(options => configure(options));
        }

        services.AddSingleton(connectionFactory);
        services.AddSingleton<ISqlDialect>(sp => 
        {
            var opts = sp.GetService<IOptions<SlimQueryOptions>>();
            var dialect = opts?.Value.Dialect ?? DatabaseDialect.SQLite;
            return CreateDialect(dialect);
        });
        services.AddSingleton<SlimQuery.Core.SlimConnection>();
        services.AddSingleton<ISlimConnection>(sp => sp.GetRequiredService<SlimQuery.Core.SlimConnection>());
        
        services.AddMemoryCache();
        services.AddSingleton<QueryCache>();

        return services;
    }

    public static IServiceCollection AddSlimQuery<TConnection>(
        this IServiceCollection services,
        Action<SlimQueryOptions>? configure = null)
        where TConnection : class, IDbConnection
    {
        return services.AddSlimQuery(() => (IDbConnection)Activator.CreateInstance<TConnection>()!, configure);
    }

    private static ISqlDialect CreateDialect(DatabaseDialect dialect)
    {
        return dialect switch
        {
            DatabaseDialect.SQLite => new SqliteDialect(),
            DatabaseDialect.PostgreSQL => new PostgresDialect(),
            DatabaseDialect.SqlServer => new SqlServerDialect(),
            DatabaseDialect.MySQL => new MySqlDialect(),
            _ => new SqliteDialect()
        };
    }
}

public class SlimQueryOptions
{
    public DatabaseDialect Dialect { get; set; } = DatabaseDialect.SQLite;
    public bool EnableCaching { get; set; } = true;
    public TimeSpan DefaultCacheTtl { get; set; } = TimeSpan.FromMinutes(5);
}

public enum DatabaseDialect
{
    SQLite,
    PostgreSQL,
    SqlServer,
    MySQL
}

public static class SlimQueryServiceExtensions
{
    public static ISlimConnection GetSlimQuery(this IServiceProvider services)
    {
        return services.GetRequiredService<ISlimConnection>();
    }

    public static ISlimConnection CreateScope(this IServiceProvider services)
    {
        return services.GetRequiredService<SlimQuery.Core.SlimConnection>();
    }
}

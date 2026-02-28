using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlimQuery.Migrations;

namespace SlimQuery.Extensions;

public class MigrationHostedService : IHostedService
{
    private readonly SlimQuery.Core.SlimConnection _connection;
    private readonly IEnumerable<Migration> _migrations;

    public MigrationHostedService(SlimQuery.Core.SlimConnection connection, IEnumerable<Migration> migrations)
    {
        _connection = connection;
        _migrations = migrations;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var manager = new MigrationManager(_connection);
        await manager.MigrateUpAsync(_migrations);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public static class MigrationHostedServiceExtensions
{
    public static IServiceCollection AddSlimQueryMigrations(
        this IServiceCollection services,
        IEnumerable<Migration> migrations)
    {
        services.AddHostedService(sp =>
        {
            var connection = sp.GetRequiredService<SlimQuery.Core.SlimConnection>();
            return new MigrationHostedService(connection, migrations);
        });
        
        return services;
    }
}

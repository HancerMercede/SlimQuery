using System.Data;
using System.Text;

namespace SlimQuery.Migrations;

public class Migration
{
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UpSql { get; set; } = string.Empty;
    public string DownSql { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
}

public class MigrationManager
{
    private readonly SlimQuery.Core.SlimConnection _connection;
    private readonly string _tableName;

    public MigrationManager(SlimQuery.Core.SlimConnection connection, string tableName = "_migrations")
    {
        _connection = connection;
        _tableName = tableName;
    }

    public async Task EnsureMigrationsTableAsync()
    {
        var sql = $@"
            CREATE TABLE IF NOT EXISTS {_tableName} (
                version INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                applied_at TEXT NOT NULL
            )";
        await _connection.ExecuteAsync(sql);
    }

    public async Task<int> GetCurrentVersionAsync()
    {
        await EnsureMigrationsTableAsync();
        var result = await _connection.ExecuteScalarAsync<int?>($"SELECT MAX(version) FROM {_tableName}");
        return result ?? 0;
    }

    public async Task<List<Migration>> GetAppliedMigrationsAsync()
    {
        await EnsureMigrationsTableAsync();
        var sql = $"SELECT version, name, applied_at FROM {_tableName} ORDER BY version";
        var results = await _connection.QueryAsync<Migration>(sql);
        return results.ToList();
    }

    public async Task ApplyMigrationAsync(Migration migration)
    {
        await _connection.ExecuteAsync(migration.UpSql);
        
        var sql = $"INSERT INTO {_tableName} (version, name, applied_at) VALUES (@Version, @Name, @AppliedAt)";
        await _connection.ExecuteAsync(sql, new 
        { 
            migration.Version, 
            migration.Name, 
            AppliedAt = DateTime.UtcNow.ToString("O") 
        });
    }

    public async Task RollbackMigrationAsync(Migration migration)
    {
        if (!string.IsNullOrEmpty(migration.DownSql))
        {
            await _connection.ExecuteAsync(migration.DownSql);
        }
        
        var sql = $"DELETE FROM {_tableName} WHERE version = @Version";
        await _connection.ExecuteAsync(sql, new { migration.Version });
    }

    public async Task MigrateUpAsync(IEnumerable<Migration> migrations)
    {
        var currentVersion = await GetCurrentVersionAsync();
        var pending = migrations.Where(m => m.Version > currentVersion).OrderBy(m => m.Version);

        foreach (var migration in pending)
        {
            await ApplyMigrationAsync(migration);
        }
    }

    public async Task MigrateDownAsync(IEnumerable<Migration> migrations, int targetVersion)
    {
        var applied = await GetAppliedMigrationsAsync();
        var toRollback = applied.Where(m => m.Version > targetVersion).OrderByDescending(m => m.Version);

        foreach (var migration in toRollback)
        {
            await RollbackMigrationAsync(migration);
        }
    }
}

public static class MigrationBuilder
{
    private static int _version = 1;
    private static readonly List<Migration> _migrations = new();

    public static Migration Create(string name, Action<Migration> configure)
    {
        var migration = new Migration
        {
            Version = _version++,
            Name = name
        };
        configure(migration);
        _migrations.Add(migration);
        return migration;
    }

    public static IEnumerable<Migration> GetMigrations() => _migrations;

    public static void Reset() => _migrations.Clear();
}

public static class MigrationExtensions
{
    public static Migration Up(this Migration migration, string sql)
    {
        migration.UpSql = sql;
        return migration;
    }

    public static Migration Down(this Migration migration, string sql)
    {
        migration.DownSql = sql;
        return migration;
    }
}

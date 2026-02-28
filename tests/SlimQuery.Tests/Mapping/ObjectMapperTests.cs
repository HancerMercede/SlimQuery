using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using SlimQuery.Core;

namespace SlimQuery.Tests;

public class ObjectMapperTests
{
    [Fact]
    public async Task QueryAsync_ShouldReturnResults()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                active INTEGER NOT NULL
            );
            INSERT INTO users (name, active) VALUES ('Alice', 1);
            INSERT INTO users (name, active) VALUES ('Bob', 0);
        ");

        var db = new SlimConnection(connection);
        var users = await db.QueryAsync<User>("SELECT * FROM users");

        Assert.Equal(2, users.Count());
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ShouldReturnFirst()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );
            INSERT INTO users (name) VALUES ('Alice');
            INSERT INTO users (name) VALUES ('Bob');
        ");

        var db = new SlimConnection(connection);
        var user = await db.QueryFirstOrDefaultAsync<User>("SELECT * FROM users WHERE name = @Name", new { Name = "Alice" });

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public async Task ExecuteScalarAsync_ShouldReturnCount()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (name TEXT);
            INSERT INTO users (name) VALUES ('Alice');
            INSERT INTO users (name) VALUES ('Bob');
        ");

        var db = new SlimConnection(connection);
        var count = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task InsertAsync_ShouldInsertAndReturnId()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                active INTEGER NOT NULL DEFAULT 1
            );
        ");

        var db = new SlimConnection(connection);
        var user = new User { Name = "Charlie", Active = true };
        var id = await db.InsertAsync(user);

        Assert.NotEqual(0, id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                active INTEGER NOT NULL
            );
            INSERT INTO users (name, active) VALUES ('Alice', 1);
        ");

        var db = new SlimConnection(connection);
        var user = await db.GetByIdAsync<User>(1);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteEntity()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );
            INSERT INTO users (name) VALUES ('Alice');
        ");

        var db = new SlimConnection(connection);
        await db.DeleteAsync<User>(1);

        var count = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");
        Assert.Equal(0, count);
    }
}

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
}

using Microsoft.Data.Sqlite;
using Dapper;
using SlimQuery.Core;
using SlimQuery.Extensions;

namespace SlimQuery.Tests;

public class QueryBuilderTests
{
    [Fact]
    public async Task QueryBuilder_ToListAsync_ReturnsResults()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                active INTEGER NOT NULL DEFAULT 1
            );
            INSERT INTO users (name, active) VALUES ('Alice', 1);
            INSERT INTO users (name, active) VALUES ('Bob', 1);
            INSERT INTO users (name, active) VALUES ('Charlie', 1);
        ");

        var db = new SlimConnection(connection);
        var results = await db.From<User>().ToListAsync();

        Assert.Equal(3, results.Count());
    }

    [Fact]
    public async Task QueryBuilder_Where_FiltersResults()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                active INTEGER NOT NULL DEFAULT 1
            );
            INSERT INTO users (name, active) VALUES ('Alice', 1);
            INSERT INTO users (name, active) VALUES ('Bob', 0);
        ");

        var db = new SlimConnection(connection);
        var results = await db.From<User>()
            .Where(x => x.Active == true)
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("Alice", results.First().Name);
    }

    [Fact]
    public async Task QueryBuilder_OrderBy_SortsResults()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                active INTEGER NOT NULL DEFAULT 1
            );
            INSERT INTO users (name, active) VALUES ('Charlie', 1);
            INSERT INTO users (name, active) VALUES ('Alice', 1);
            INSERT INTO users (name, active) VALUES ('Bob', 1);
        ");

        var db = new SlimConnection(connection);
        var results = (await db.From<User>()
            .OrderBy(x => x.Name)
            .ToListAsync()).ToList();

        Assert.Equal("Alice", results[0].Name);
        Assert.Equal("Bob", results[1].Name);
        Assert.Equal("Charlie", results[2].Name);
    }

    [Fact]
    public async Task QueryBuilder_SkipTake_PaginatesResults()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                active INTEGER NOT NULL DEFAULT 1
            );
            INSERT INTO users (name, active) VALUES ('A', 1);
            INSERT INTO users (name, active) VALUES ('B', 1);
            INSERT INTO users (name, active) VALUES ('C', 1);
            INSERT INTO users (name, active) VALUES ('D', 1);
        ");

        var db = new SlimConnection(connection);
        var results = (await db.From<User>()
            .OrderBy(x => x.Name)
            .Skip(1)
            .Take(2)
            .ToListAsync()).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("B", results[0].Name);
        Assert.Equal("C", results[1].Name);
    }

    [Fact]
    public async Task QueryBuilder_CountAsync_ReturnsCount()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                active INTEGER NOT NULL DEFAULT 1
            );
            INSERT INTO users (name, active) VALUES ('A', 1);
            INSERT INTO users (name, active) VALUES ('B', 1);
            INSERT INTO users (name, active) VALUES ('C', 1);
        ");

        var db = new SlimConnection(connection);
        var count = await db.From<User>().CountAsync();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Pagination_PageSize_ReturnsCorrectMetadata()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                active INTEGER NOT NULL DEFAULT 1
            );
            INSERT INTO users (name, active) VALUES ('A', 1);
            INSERT INTO users (name, active) VALUES ('B', 1);
            INSERT INTO users (name, active) VALUES ('C', 1);
            INSERT INTO users (name, active) VALUES ('D', 1);
            INSERT INTO users (name, active) VALUES ('E', 1);
        ");

        var db = new SlimConnection(connection);
        var paged = await db.From<User>()
            .OrderBy(x => x.Name)
            .PaginateAsync(page: 2, pageSize: 2);

        Assert.Equal(2, paged.Items.Count);
        Assert.Equal(2, paged.Page);
        Assert.Equal(2, paged.PageSize);
        Assert.Equal(5, paged.TotalCount);
        Assert.Equal(3, paged.TotalPages);
        Assert.True(paged.HasPrevious);
        Assert.True(paged.HasNext);
    }

    [Fact]
    public async Task Pagination_InvalidPage_DefaultsToOne()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                active INTEGER NOT NULL DEFAULT 1
            );
            INSERT INTO users (name, active) VALUES ('A', 1);
        ");

        var db = new SlimConnection(connection);
        var paged = await db.From<User>()
            .PaginateAsync(page: -1, pageSize: 10);

        Assert.Equal(1, paged.Page);
    }
}

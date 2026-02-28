# SlimQuery

A high-performance micro ORM for .NET 10 that combines Dapper's speed with EF Core-like ergonomics.

## Features

- **Fast Performance** - Built on Dapper, the fastest .NET ORM
- **No Attributes Required** - Plain POCOs, no decorations needed
- **Multi-Database Support** - SQLite, PostgreSQL, SQL Server, MySQL
- **Fluent Query Builder** - Chainable LINQ-like syntax
- **Pagination** - Built-in paging with metadata
- **Relations** - HasOne, HasMany with batched loading (no N+1)
- **Bulk Operations** - Efficient batch inserts/updates/deletes
- **Transactions** - Full ACID support with savepoints
- **Caching** - Query result caching with tag-based invalidation
- **Migrations** - Built-in migration system
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection integration

## Quick Start

```csharp
using SlimQuery.Core;
using Microsoft.Data.Sqlite;

// Create connection
using var connection = new SqliteConnection("Data Source=mydb.sqlite");
await connection.OpenAsync();

// Wrap with SlimQuery
var db = new SlimQuery.Core.SlimConnection(connection);

// Query
var users = await db.QueryAsync<User>("SELECT * FROM users WHERE active = @Active", new { Active = true });

// CRUD
var user = await db.GetByIdAsync<User>(1);
var id = await db.InsertAsync(new User { Name = "John" });
await db.UpdateAsync(user);
await db.DeleteAsync<User>(1);

// Fluent Query Builder
var users = await db.From<User>()
    .Where(x => x.Active == true)
    .OrderBy(x => x.Name)
    .Skip(0).Take(10)
    .ToListAsync();

// Pagination
var paged = await db.From<User>()
    .Where(x => x.Active == true)
    .PaginateAsync(page: 1, pageSize: 20);
```

## Installation

```bash
dotnet add package SlimQuery
```

## Supported Databases

- SQLite (Microsoft.Data.Sqlite)
- PostgreSQL (Npgsql)
- SQL Server (Microsoft.Data.SqlClient)
- MySQL (MySqlConnector)

## Project Structure

```
src/SlimQuery/
├── Core/              # ISlimConnection, SlimConnection, SlimTransaction
├── Mapping/           # ObjectMapper, MapperCache, TypeConverters
├── Query/             # QueryBuilder, ExpressionExtractor
│   └── SqlDialect/    # Database-specific SQL generation
├── Relations/         # RelationConfig, RelationLoader
├── Bulk/              # BulkOperations
├── Cache/             # QueryCache
├── Migrations/        # MigrationManager
├── Extensions/        # DI extensions, Pagination
```

## License

MIT

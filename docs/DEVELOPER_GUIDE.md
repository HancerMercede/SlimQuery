# SlimQuery Developer Guide

## Table of Contents

1. [Getting Started](#getting-started)
2. [Configuration](#configuration)
3. [Raw SQL Operations](#raw-sql-operations)
4. [CRUD Operations](#crud-operations)
5. [Query Builder](#query-builder)
6. [Pagination](#pagination)
7. [Relations](#relations)
8. [Bulk Operations](#bulk-operations)
9. [Transactions](#transactions)
10. [Caching](#caching)
11. [Migrations](#migrations)
12. [Dependency Injection](#dependency-injection)
13. [Best Practices](#best-practices)
14. [Architecture](#architecture)
15. [Contributing](#contributing)

---

## Getting Started

### Installation

```bash
dotnet add package SlimQuery
dotnet add package Microsoft.Data.Sqlite  # or Npgsql, Microsoft.Data.SqlClient, MySqlConnector
```

### Quick Setup

```csharp
using SlimQuery.Core;
using Microsoft.Data.Sqlite;

// Create and open connection
using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

// Wrap with SlimQuery
var db = new SlimConnection(connection);
```

---

## Configuration

### Database Dialects

SlimQuery supports multiple databases. Set the dialect based on your provider:

```csharp
// SQLite
var dialect = SqlDialects.SQLite;

// PostgreSQL
var dialect = SqlDialects.PostgreSQL;

// SQL Server
var dialect = SqlDialects.SqlServer;

// MySQL
var dialect = SqlDialects.MySQL;
```

### Connection String Options

```csharp
// SQLite
"Data Source=app.db"

// PostgreSQL
"Host=localhost;Database=mydb;Username=user;Password=pass"

// SQL Server
"Server=localhost;Database=mydb;Trusted_Connection=True;TrustServerCertificate=True"

// MySQL
"Server=localhost;Database=mydb;User=user;Password=pass"
```

---

## Raw SQL Operations

### Querying with Parameters

```csharp
var db = new SlimConnection(connection);

// Simple query
var users = await db.QueryAsync<User>("SELECT * FROM users");

// With parameters
var activeUsers = await db.QueryAsync<User>(
    "SELECT * FROM users WHERE active = @Active", 
    new { Active = true }
);

// Single result
var user = await db.QueryFirstOrDefaultAsync<User>(
    "SELECT * FROM users WHERE id = @Id",
    new { Id = 1 }
);

// Scalar value
var count = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");
var name = await db.ExecuteScalarAsync<string>("SELECT name FROM users WHERE id = @Id", new { Id = 1 });
```

### Executing Commands

```csharp
// Insert/Update/Delete
var rowsAffected = await db.ExecuteAsync(
    "UPDATE users SET name = @Name WHERE id = @Id",
    new { Name = "John", Id = 1 }
);

// Multiple commands in one call
await db.ExecuteAsync(new[]
{
    "INSERT INTO logs (msg) VALUES ('start')",
    "INSERT INTO logs (msg) VALUES ('end')"
});
```

### Dynamic Results

```csharp
// Dynamic objects
var results = await db.QueryAsync("SELECT * FROM users");
foreach (var row in results)
{
    var name = row.Name;
    var id = row.Id;
}
```

---

## CRUD Operations

### Get by ID

```csharp
var user = await db.GetByIdAsync<User>(1);
```

### Insert

```csharp
// Insert and get generated ID
var user = new User { Name = "John", Email = "john@example.com", Active = true };
var id = await db.InsertAsync(user);

// Insert with specific ID
await db.InsertAsync(user, useGeneratedKey: false);
```

### Update

```csharp
var user = await db.GetByIdAsync<User>(1);
user.Name = "Jane";
await db.UpdateAsync(user);

// Update specific columns only
await db.UpdateAsync(user, columns: new[] { "Name", "Email" });
```

### Delete

```csharp
// By ID
await db.DeleteAsync<User>(1);

// With where condition
await db.ExecuteAsync("DELETE FROM users WHERE active = @Active", new { Active = false });
```

### Upsert

```csharp
// Insert or update based on existence
await db.UpsertAsync(user);
```

---

## Query Builder

### Basic Filtering

```csharp
var users = await db.From<User>()
    .Where(x => x.Active == true)
    .ToListAsync();

// Multiple conditions (AND)
var users = await db.From<User>()
    .Where(x => x.Active == true && x.Role == "admin")
    .ToListAsync();
```

### Ordering

```csharp
// Ascending
var users = await db.From<User>()
    .OrderBy(x => x.Name)
    .ToListAsync();

// Descending
var users = await db.From<User>()
    .OrderByDesc(x => x.CreatedAt)
    .ToListAsync();

// Multiple orderings
var users = await db.From<User>()
    .OrderBy(x => x.Role)
    .OrderByDesc(x => x.Name)
    .ToListAsync();
```

### Pagination

```csharp
var users = await db.From<User>()
    .OrderBy(x => x.Name)
    .Skip(20)
    .Take(10)
    .ToListAsync();
```

### Select / Projection

```csharp
// Select specific columns
var items = await db.From<User>()
    .Select(x => new { x.Id, x.Name })
    .ToListAsync();

// Select into different type
var dtos = await db.From<User>()
    .Select(x => new UserDto { UserId = x.Id, FullName = x.Name })
    .ToListAsync();
```

### Count

```csharp
var count = await db.From<User>()
    .Where(x => x.Active == true)
    .CountAsync();
```

### First / Single

```csharp
var user = await db.From<User>()
    .Where(x => x.Id == 1)
    .FirstAsync();

var user = await db.From<User>()
    .Where(x => x.Email == "john@example.com")
    .FirstOrDefaultAsync();
```

### Complex Queries

```csharp
var results = await db.From<User>()
    .Where(x => 
        x.Active == true && 
        (x.Role == "admin" || x.Role == "moderator"))
    .OrderByDesc(x => x.CreatedAt)
    .Skip(0)
    .Take(50)
    .Select(x => new { x.Id, x.Name, x.Email })
    .ToListAsync();
```

---

## Pagination

### Using PaginateAsync

```csharp
var paged = await db.From<User>()
    .Where(x => x.Active == true)
    .OrderBy(x => x.Name)
    .PaginateAsync(page: 2, pageSize: 20);

// Access results
Console.WriteLine($"Page {paged.Page} of {paged.TotalPages}");
Console.WriteLine($"Total: {paged.TotalCount} items");
Console.WriteLine($"Has previous: {paged.HasPrevious}");
Console.WriteLine($"Has next: {paged.HasNext}");

var items = paged.Items;
```

### Manual Pagination

```csharp
var count = await db.From<User>().Where(x => x.Active).CountAsync();
var items = await db.From<User>()
    .Where(x => x.Active)
    .OrderBy(x => x.Name)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

## Relations

### Configuration

Configure relations at application startup:

```csharp
// One-to-Many (User has many Orders)
RelationBuilder.HasMany<User>(u => u.Orders)
    .WithForeignKey("user_id");

// One-to-One (User has one Profile)
RelationBuilder.HasOne<User>(u => u.Profile)
    .WithForeignKey("user_id");

// Many-to-One (Order belongs to User)
RelationBuilder.HasMany<Order>(o => o.User)
    .WithForeignKey("user_id");
```

### Model Definitions

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Order> Orders { get; set; }
    public UserProfile Profile { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public User User { get; set; }
}

public class UserProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Bio { get; set; }
}
```

### Loading Relations

```csharp
// Include one relation
var users = await db.From<User>()
    .Include(x => x.Profile)
    .ToListAsync();

// Include multiple relations
var users = await db.From<User>()
    .Include(x => x.Profile)
    .Include(x => x.Orders)
    .ToListAsync();

// Include with where condition
var users = await db.From<User>()
    .Include(x => x.Orders.Where(o => o.Total > 100))
    .ToListAsync();

// Nested includes
var users = await db.From<User>()
    .Include(x => x.Orders.Select(o => o.OrderItems))
    .ToListAsync();
```

---

## Bulk Operations

### Bulk Insert

```csharp
var bulk = new BulkOperations(db, dialect);

// Simple bulk insert
var users = GenerateUsers(); // List<User>
await bulk.BulkInsertAsync(users);

// Batched bulk insert (for large datasets)
await bulk.BulkInsertBatchedAsync(users, batchSize: 1000);
```

### Bulk Update

```csharp
var users = await db.From<User>().ToListAsync();
// Modify users...
await bulk.BulkUpdateAsync(users);

// Bulk update with specific columns
await bulk.BulkUpdateAsync(users, columns: new[] { "Name", "Email" });
```

### Bulk Delete

```csharp
// By IDs
var ids = new[] { 1, 2, 3, 4, 5 };
await bulk.BulkDeleteByIdsAsync<User>(ids);

// By condition
await bulk.BulkDeleteAsync<User>(x => x.Active == false);
```

---

## Transactions

### Basic Transaction

```csharp
using var tx = await db.BeginTransactionAsync();
try
{
    await db.ExecuteAsync("INSERT INTO orders (total) VALUES (@Total)", 
        new { Total = 100 }, tx.Transaction);
    await db.ExecuteAsync("UPDATE accounts SET balance = balance - @Amount WHERE id = @Id",
        new { Amount = 100, Id = 1 }, tx.Transaction);
    
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

### Async Disposable Pattern

```csharp
await using var tx = await db.BeginTransactionAsync();
await db.ExecuteAsync("...", param, tx.Transaction);
await tx.CommitAsync();
// Auto-rollback if not committed
```

### Savepoints

```csharp
using var tx = await db.BeginTransactionAsync();
await db.ExecuteAsync("INSERT INTO logs (msg) VALUES ('step1')");

await tx.SavepointAsync("after_step1");

await db.ExecuteAsync("INSERT INTO logs (msg) VALUES ('step2')");
await tx.RollbackToSavepointAsync("after_step1"); // Keeps step1, removes step2

await tx.CommitAsync();
```

### Nested Transactions

```csharp
// Each call creates a real transaction
using var tx1 = await db.BeginTransactionAsync();
await db.ExecuteAsync("...");

// Nested - creates separate transaction
using var tx2 = await db.BeginTransactionAsync();
await db.ExecuteAsync("...");
await tx2.CommitAsync();

await tx1.CommitAsync();
```

---

## Caching

### Enabling Caching

```csharp
// Per-query cache
var users = await db.From<User>()
    .Where(x => x.Active)
    .Cache(TimeSpan.FromMinutes(5))
    .ToListAsync();

// Cache with custom key
var users = await db.From<User>()
    .Where(x => x.Role == "admin")
    .Cache(TimeSpan.FromMinutes(10), "admin_users")
    .ToListAsync();
```

### Cache Invalidation

```csharp
var cache = new QueryCache();

// Invalidate by key
cache.Invalidate("admin_users");

// Invalidate by tag
cache.InvalidateByTag("users");

// Clear all cache
cache.Clear();
```

### Cache with Tags

```csharp
// Add tags to cache entry
var users = await db.From<User>()
    .Where(x => x.Active)
    .Cache(TimeSpan.FromMinutes(5), tags: new[] { "users", "active" })
    .ToListAsync();

// Invalidate by tag
cache.InvalidateByTag("users");
```

---

## Migrations

### Migration Files

Create SQL migration files in your project:

```sql
-- migrations/001_create_users.sql
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL,
    active INTEGER DEFAULT 1,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
```

### Running Migrations

```csharp
var manager = new MigrationManager(db);

// Get all migration files
var migrations = Directory.GetFiles("migrations", "*.sql")
    .Select(f => new MigrationFile(Path.GetFileName(f), File.ReadAllText(f)))
    .OrderBy(m => m.Name)
    .ToList();

// Apply pending migrations
await manager.MigrateUpAsync(migrations);
```

### Migration Builder

```csharp
MigrationBuilder.Create("001_create_users", m => m
    .Up(@"
        CREATE TABLE users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL
        )")
    .Down("DROP TABLE users"));
```

### Rolling Back

```csharp
// Rollback to specific version
await manager.MigrateDownAsync(migrations, targetVersion: "000");

// Rollback last migration
await manager.MigrateDownAsync(migrations, steps: 1);
```

---

## Dependency Injection

### Basic Registration

```csharp
// In Program.cs
services.AddSlimQuery(() => new SqliteConnection("Data Source=app.db"));
```

### With Options

```csharp
services.AddSlimQuery(
    () => new SqliteConnection(connectionString),
    options =>
    {
        options.Dialect = DatabaseDialect.SQLite;
        options.EnableCaching = true;
        options.CommandTimeout = 30;
    }
);
```

### Using in Services

```csharp
public class UserService
{
    private readonly ISlimConnection _db;
    
    public UserService(ISlimConnection db)
    {
        _db = db;
    }
    
    public async Task<User> GetByIdAsync(int id)
    {
        return await _db.GetByIdAsync<User>(id);
    }
}
```

### With Hosted Service for Migrations

```csharp
// Register migration hosted service
services.AddHostedService<MigrationHostedService>();

// Configure in options
services.AddSlimQuery(() => new SqliteConnection("Data Source=app.db"), options =>
{
    options.AutoMigrate = true;
    options.MigrationPath = "migrations";
});
```

---

## Best Practices

### 1. Always Use Parameterized Queries

```csharp
// Good - parameterized
await db.QueryAsync("SELECT * FROM users WHERE id = @Id", new { Id = userId });

// Bad - string interpolation (SQL injection risk!)
await db.QueryAsync($"SELECT * FROM users WHERE id = {userId}");
```

### 2. Use Async/Await

```csharp
// Good
var users = await db.From<User>().ToListAsync();

// Avoid
var users = db.From<User>().ToList(); // Synchronous
```

### 3. Dispose Connections

```csharp
// Good - using statement
using var connection = new SqliteConnection(connectionString);
var db = new SlimConnection(connection);
// Connection auto-disposed

// Bad - manual management
var connection = new SqliteConnection(connectionString);
// ... forget to dispose
```

### 4. Use Query Builder for Complex Queries

```csharp
// Good - readable and safe
var results = await db.From<Order>()
    .Where(x => x.Status == "pending")
    .Where(x => x.Total > threshold)
    .OrderByDesc(x => x.CreatedAt)
    .Skip(offset)
    .Take(pageSize)
    .Select(x => new { x.Id, x.Total, x.Status })
    .ToListAsync();

// Better for complex dynamic queries - raw SQL
var sql = BuildDynamicSql(filters, sorting, pagination);
var results = await db.QueryAsync<Order>(sql, parameters);
```

### 5. Configure Relations Once at Startup

```csharp
// In Program.cs or startup configuration
public void ConfigureServices(IServiceCollection services)
{
    // Configure all relations here - once
    RelationBuilder.HasMany<User>(u => u.Orders).WithForeignKey("user_id");
    RelationBuilder.HasOne<User>(u => u.Profile).WithForeignKey("user_id");
    
    services.AddSlimQuery(...);
}
```

### 6. Use Bulk Operations for Large Datasets

```csharp
// Good - bulk insert
var users = GenerateLargeList();
await bulk.BulkInsertAsync(users);

// Bad - loop inserts
foreach (var user in users)
{
    await db.InsertAsync(user); // N database calls!
}
```

### 7. Use Transactions for Multi-Statement Operations

```csharp
// Good - atomic operation
using var tx = await db.BeginTransactionAsync();
try
{
    await db.ExecuteAsync("INSERT INTO orders ...", orderParams);
    await db.ExecuteAsync("UPDATE inventory ...", inventoryParams);
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

### 8. Leverage Caching Wisely

```csharp
// Good - cache frequently accessed, rarely changing data
var roles = await db.From<Role>()
    .Cache(TimeSpan.FromHours(1))
    .ToListAsync();

// Avoid - cache frequently changing data
var user = await db.From<User>()
    .Where(x => x.Id == id)
    .Cache(TimeSpan.FromMinutes(5)) // User data changes often!
    .FirstAsync();
```

### 9. Handle CancellationToken

```csharp
public async Task<User> GetUserAsync(int id, CancellationToken ct = default)
{
    return await db.GetByIdAsync<User>(id, cancellationToken: ct);
}

public async Task<List<User>> SearchUsersAsync(string term, CancellationToken ct = default)
{
    return await db.From<User>()
        .Where(x => x.Name.Contains(term))
        .ToListAsync(cancellationToken: ct);
}
```

### 10. Use Appropriate Return Types

```csharp
// Use FirstOrDefault when 0 or 1 expected
var user = await db.From<User>()
    .Where(x => x.Email == email)
    .FirstOrDefaultAsync();

// Use First when 1+ expected (throws if none)
var user = await db.From<User>()
    .Where(x => x.Id == id)
    .FirstAsync();

// Use ToListAsync when 0+ expected
var users = await db.From<User>()
    .Where(x => x.Active)
    .ToListAsync();
```

---

## Architecture

```
┌─────────────────────────────────────┐
│           Application               │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│         ISlimConnection              │
│  (Query, CRUD, Transactions)        │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│           Dapper                    │
│  (SQL Execution, Parameter Mapping) │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│        IDbConnection                │
│   (SQLite, PostgreSQL, etc.)        │
└─────────────────────────────────────┘
```

### Design Principles

1. **No Attributes Required** - Use plain POCOs
2. **Convention over Configuration** - Auto table/column naming
3. **Performance First** - Compiled expression mappers
4. **Multi-Database** - Swappable SQL dialects
5. **No Change Tracking** - Explicit updates only

---

## Contributing

### Running Tests

```bash
dotnet test
```

### Building

```bash
dotnet build
```

### Adding a New Feature

1. Create feature branch
2. Add code in appropriate module
3. Write unit tests
4. Update documentation
5. Submit PR

### Code Style

- Use C# 12 features where appropriate
- Prefer async/await over synchronous calls
- Add XML docs for public APIs
- Keep nullability warnings to zero

---

## FAQ

**Q: How is this different from Dapper?**
A: SlimQuery adds a fluent query builder, CRUD helpers, relations, migrations, and DI on top of Dapper.

**Q: Do I need attributes on my models?**
A: No! SlimQuery uses conventions (e.g., `User` → `users` table).

**Q: Which databases are supported?**
A: SQLite, PostgreSQL, SQL Server, and MySQL.

**Q: Does SlimQuery support change tracking?**
A: No. SlimQuery is explicit-only. You update what you want, when you want. No hidden state.

**Q: Is lazy loading supported?**
A: No. All relation loading is explicit via `Include()`. This prevents N+1 query problems.

# SlimQuery Developer Guide

## Table of Contents

1. [Architecture](#architecture)
2. [Project Structure](#project-structure)
3. [Building Blocks](#building-blocks)
4. [Contributing](#contributing)

---

## Architecture

SlimQuery is a micro ORM that sits between raw ADO.NET and full ORMs like Entity Framework Core.

```
┌─────────────────────────────────────┐
│           Application               │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│         ISlimConnection             │
│  (Query, CRUD, Transactions)        │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│           Dapper                     │
│  (SQL Execution, Parameter Mapping)  │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│        IDbConnection                 │
│   (SQLite, PostgreSQL, etc.)        │
└─────────────────────────────────────┘
```

### Design Principles

1. **No Attributes Required** - Use plain POCOs
2. **Convention over Configuration** - Auto table/column naming
3. **Performance First** - Compiled expression mappers
4. **Multi-Database** - Swappable SQL dialects

---

## Project Structure

```
src/SlimQuery/
├── Core/
│   ├── ISlimConnection.cs      # Main interface
│   ├── SlimConnection.cs       # Implementation
│   └── SlimTransaction.cs      # Transaction wrapper
│
├── Mapping/
│   ├── ObjectMapper.cs         # Compiled expression mapper
│   ├── MapperCache.cs          # Mapper caching
│   └── TypeConverters.cs       # Type conversions
│
├── Query/
│   ├── IQueryBuilder.cs        # Query builder interface
│   ├── QueryBuilder.cs         # Fluent query implementation
│   ├── QueryContext.cs         # Query state & expression parsing
│   └── SqlDialect/
│       ├── ISqlDialect.cs      # Dialect interface
│       └── SqlDialects.cs      # SQLite, PostgreSQL, SQLServer, MySQL
│
├── Relations/
│   ├── RelationConfig.cs       # FK metadata
│   └── RelationLoader.cs       # Batched relation loading
│
├── Bulk/
│   └── BulkOperations.cs       # Bulk insert/update/delete
│
├── Cache/
│   └── QueryCache.cs           # Query result caching
│
├── Migrations/
│   └── MigrationManager.cs    # Migration system
│
└── Extensions/
    ├── SlimQueryExtensions.cs      # DI registration
    ├── MigrationHostedService.cs   # Auto-migrations
    └── PaginationExtensions.cs     # Pagination utilities
```

---

## Building Blocks

### 1. Core - SlimConnection

Main entry point for all database operations:

```csharp
var db = new SlimQuery.Core.SlimConnection(connection);

// Raw SQL
await db.QueryAsync<T>(sql, param);
await db.ExecuteAsync(sql, param);

// CRUD
await db.GetByIdAsync<T>(id);
await db.InsertAsync(entity);
await db.UpdateAsync(entity);
await db.DeleteAsync<T>(id);

// Transactions
using var tx = await db.BeginTransactionAsync();
await db.ExecuteAsync(sql, param, tx.Transaction);
await tx.CommitAsync();
```

### 2. Query Builder

Fluent API for building queries:

```csharp
var results = await db.From<User>()
    .Where(x => x.Active == true)
    .OrderBy(x => x.Name)
    .Skip(10).Take(20)
    .Select(x => new { x.Id, x.Name })
    .ToListAsync();
```

**Supported Operations:**
- `Where(Expression)` - Filter conditions
- `OrderBy(Expression)` / `OrderByDesc(Expression)` - Sorting
- `Skip(int)` / `Take(int)` - Pagination
- `Select(Expression)` - Projection
- `Include<T>(Expression)` - Relation loading
- `Cache(TimeSpan, string?)` - Result caching
- `ToListAsync()` / `FirstAsync()` / `CountAsync()` - Execution

### 3. Relations

Configure relations at startup:

```csharp
// One-to-Many
RelationBuilder.HasMany<User>(u => u.Orders).WithForeignKey("user_id");

// One-to-One
RelationBuilder.HasOne<User>(u => u.Profile).WithForeignKey("user_id");
```

Query with includes:

```csharp
var users = await db.From<User>()
    .Include<Order>(x => x.Orders)
    .ToListAsync();
```

### 4. Bulk Operations

```csharp
var bulk = new BulkOperations(db, dialect);

// Bulk insert
await bulk.BulkInsertAsync(entities);
await bulk.BulkInsertBatchedAsync(entities, batchSize: 1000);

// Bulk update/delete
await bulk.BulkUpdateAsync(entities);
await bulk.BulkDeleteByIdsAsync<User>(ids);
```

### 5. Migrations

```csharp
var manager = new MigrationManager(db);

// Apply pending migrations
await manager.MigrateUpAsync(migrations);

// Rollback
await manager.MigrateDownAsync(migrations, targetVersion);
```

Fluent migration definition:

```csharp
MigrationBuilder.Create("create_users", m => m
    .Up("CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT)")
    .Down("DROP TABLE users"));
```

### 6. Dependency Injection

```csharp
services.AddSlimQuery(() => new SqliteConnection("Data Source=app.db"), options =>
{
    options.Dialect = DatabaseDialect.SQLite;
    options.EnableCaching = true;
});

// Use
var db = serviceProvider.GetSlimQuery();
```

### 7. Pagination

```csharp
// With QueryBuilder
var paged = await db.From<User>()
    .Where(x => x.Active)
    .PaginateAsync(page: 1, pageSize: 20);

// Result contains
paged.Items        // List<T>
paged.Page         // 1
paged.PageSize    // 20
paged.TotalCount  // 100
paged.TotalPages  // 5
paged.HasPrevious // false
paged.HasNext     // true
```

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

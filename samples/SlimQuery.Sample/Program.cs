using Microsoft.Data.Sqlite;
using Dapper;
using SlimQuery.Core;
using SlimQuery.Extensions;

Console.WriteLine("SlimQuery Sample Application\n");

using var connection = new SqliteConnection("Data Source=sample.db");
await connection.OpenAsync();

await connection.ExecuteAsync(@"
    DROP TABLE IF EXISTS orders;
    DROP TABLE IF EXISTS users;
    
    CREATE TABLE users (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        email TEXT NOT NULL,
        active INTEGER NOT NULL DEFAULT 1,
        created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
    );
    
    CREATE TABLE orders (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        user_id INTEGER NOT NULL,
        amount REAL NOT NULL,
        status TEXT NOT NULL DEFAULT 'pending',
        created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (user_id) REFERENCES users(id)
    );
");

var db = new SlimConnection(connection);

Console.WriteLine("=== Create Users ===");
var userId1 = await db.InsertAsync(new User { Name = "John Doe", Email = "john@example.com", Active = true });
var userId2 = await db.InsertAsync(new User { Name = "Jane Smith", Email = "jane@example.com", Active = true });
Console.WriteLine($"Created users with IDs: {userId1}, {userId2}");

Console.WriteLine("\n=== Query Users (Raw SQL) ===");
var users = await db.QueryAsync<User>("SELECT * FROM users WHERE active = @Active", new { Active = true });
foreach (var u in users)
{
    Console.WriteLine($"  {u.Id}: {u.Name} ({u.Email})");
}

Console.WriteLine("\n=== Query Users (Query Builder) ===");
var activeUsers = await db.From<User>()
    .Where(x => x.Active)
    .OrderBy(x => x.Name)
    .ToListAsync();
foreach (var u in activeUsers)
{
    Console.WriteLine($"  {u.Id}: {u.Name}");
}

Console.WriteLine("\n=== Create Orders ===");
await connection.ExecuteAsync(@"
    INSERT INTO orders (user_id, amount, status) VALUES (@UserId, @Amount, @Status)",
    new[] {
        new { UserId = userId1, Amount = 99.99, Status = "completed" },
        new { UserId = userId1, Amount = 49.50, Status = "pending" },
        new { UserId = userId2, Amount = 150.00, Status = "completed" }
    });
Console.WriteLine("Created 3 orders");

Console.WriteLine("\n=== Pagination ===");
var paged = await db.From<User>()
    .OrderBy(x => x.Name)
    .PaginateAsync(page: 1, pageSize: 2);
Console.WriteLine($"Page {paged.Page} of {paged.TotalPages}");
Console.WriteLine($"Total: {paged.TotalCount} users");
foreach (var u in paged.Items)
{
    Console.WriteLine($"  {u.Name}");
}

Console.WriteLine("\n=== Count ===");
var count = await db.From<User>().CountAsync();
Console.WriteLine($"Total users: {count}");

Console.WriteLine("\n=== Update ===");
var userToUpdate = await db.GetByIdAsync<User>(userId1);
if (userToUpdate != null)
{
    userToUpdate.Email = "john.doe@newemail.com";
    await db.UpdateAsync(userToUpdate);
    Console.WriteLine($"Updated user {userId1} email");
}

Console.WriteLine("\n✅ Sample completed successfully!");

public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Active { get; set; }
}

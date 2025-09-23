using Dapper;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;

public static class UserService
{
    private const string DbFile = "app.db";
    private static readonly object _lockObj = new(); // برای جلوگیری از race condition

    // ایجاد جدول Users
    public static async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Phone TEXT NOT NULL,
            Code TEXT NOT NULL
        );";

        await connection.ExecuteAsync(sql);
    }

    // ایجاد کاربر
    public static async Task CreateUserAsync(string name, string phone, string code)
    {
        await InitializeDatabaseAsync();

        lock (_lockObj)
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            connection.Open();

            var sql = "INSERT INTO Users (Name, Phone, Code) VALUES (@Name, @Phone, @Code)";
            connection.Execute(sql, new { Name = name, Phone = phone, Code = code });
        }
    }

    // گرفتن همه کاربران (اختیاری)
    public static async Task<List<User>> GetAllUsersAsync()
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, Name, Phone, Code FROM Users";
        var result = await connection.QueryAsync<User>(sql);
        return result.ToList();
    }
}

// مدل کاربر
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Code { get; set; } = "";
}

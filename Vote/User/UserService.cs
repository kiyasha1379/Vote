using Dapper;
using Microsoft.Data.Sqlite;

public static class UserService
{
    private const string DbFile = "app.db";

    public static void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = @"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Phone TEXT NOT NULL,
            Code TEXT NOT NULL
        );";

        connection.Execute(sql);
    }

    public static void CreateUser(string name, string phone, string code)
    {
        InitializeDatabase();
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "INSERT INTO Users (Name, Phone, Code) VALUES (@Name, @Phone, @Code)";
        connection.Execute(sql, new { Name = name, Phone = phone, Code = code });
    }
}

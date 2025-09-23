using Microsoft.Data.Sqlite;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

public static class CodeService
{
    private const string DbFile = "app.db";

    // این متد باید اول اجرا بشه تا جدول ایجاد شود
    public static void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = @"
        CREATE TABLE IF NOT EXISTS Codes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Code TEXT NOT NULL,
            IsUsed INTEGER NOT NULL DEFAULT 0
        );";

        connection.Execute(sql);
    }

    public static List<string> GenerateCodes(int count)
    {
        InitializeDatabase(); // مطمئن می‌شویم جدول وجود دارد

        var codes = new List<string>();
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        for (int i = 0; i < count; i++)
        {
            var code = GenerateRandomCode(8);
            codes.Add(code);
            connection.Execute("INSERT INTO Codes (Code, IsUsed) VALUES (@Code, 0)", new { Code = code });
        }

        return codes;
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static void ClearCodes()
    {
        InitializeDatabase(); // مطمئن می‌شویم جدول وجود دارد
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();
        connection.Execute("DELETE FROM Codes");
    }

    public static List<(string Code, bool IsUsed)> GetAllCodes()
    {
        InitializeDatabase();
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();
        return connection.Query<(string Code, bool IsUsed)>("SELECT Code, IsUsed FROM Codes").ToList();
    }

}

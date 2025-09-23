using Microsoft.Data.Sqlite;
using Dapper;
using System.Collections.Generic;

public static class GoldenRefereeService
{
    private const string DbFile = "referees.db";

    public static void EnsureDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = @"
        CREATE TABLE IF NOT EXISTS GoldenReferees (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Code TEXT NOT NULL
        );";

        connection.Execute(sql);
    }

    public static string CreateReferee(string name)
    {
        EnsureDatabase();

        var code = GenerateRandomCode(8);

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "INSERT INTO GoldenReferees (Name, Code) VALUES (@Name, @Code)";
        connection.Execute(sql, new { Name = name, Code = code });

        return code;
    }

    public static List<(string Name, string Code)> GetAllReferees()
    {
        EnsureDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "SELECT Name, Code FROM GoldenReferees";
        var result = connection.Query<(string Name, string Code)>(sql);
        return result.AsList();
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(System.Linq.Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    public static void DeleteReferee(string name)
    {
        EnsureDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "DELETE FROM GoldenReferees WHERE Name = @Name";
        connection.Execute(sql, new { Name = name });
    }

}

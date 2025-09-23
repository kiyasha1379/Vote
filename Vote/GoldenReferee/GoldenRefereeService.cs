using Microsoft.Data.Sqlite;
using Dapper;


public static class GoldenRefereeService
{
    private const string DbFile = "referees.db";

    public static void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = @"
        CREATE TABLE IF NOT EXISTS GoldenReferees (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE,
            Code TEXT NOT NULL
        );";

        connection.Execute(sql);
    }

    public static string CreateReferee(string name)
    {
        InitializeDatabase();

        string code = GenerateRandomCode(8);

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "INSERT INTO GoldenReferees (Name, Code) VALUES (@Name, @Code)";
        connection.Execute(sql, new { Name = name, Code = code });

        return code;
    }

    public static List<GoldenReferee> GetAllReferees()
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "SELECT Id, Name, Code FROM GoldenReferees";
        return connection.Query<GoldenReferee>(sql).ToList();
    }

    public static void DeleteReferee(string name)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "DELETE FROM GoldenReferees WHERE Name = @Name";
        connection.Execute(sql, new { Name = name });
    }

    public static GoldenReferee? GetRefereeByCode(string code)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "SELECT Id, Name, Code FROM GoldenReferees WHERE Code = @Code";
        return connection.QuerySingleOrDefault<GoldenReferee>(sql, new { Code = code });
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}

public class GoldenReferee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}

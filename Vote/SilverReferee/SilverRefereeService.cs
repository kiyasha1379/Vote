using Microsoft.Data.Sqlite;
using Dapper;


public static class SilverRefereeService
{
    private static readonly string DbFile = "referees.db";

    private static void EnsureDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = @"CREATE TABLE IF NOT EXISTS SilverReferees (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Code TEXT NOT NULL
                    );";

        connection.Execute(sql);
    }

    public static string CreateReferee(string name)
    {
        EnsureDatabase();

        string code = GenerateRandomCode(8);

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "INSERT INTO SilverReferees (Name, Code) VALUES (@Name, @Code)";
        connection.Execute(sql, new { Name = name, Code = code });

        return code;
    }

    public static void DeleteReferee(string name)
    {
        EnsureDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "DELETE FROM SilverReferees WHERE Name = @Name";
        connection.Execute(sql, new { Name = name });
    }

    public static List<(string Name, string Code)> GetAllReferees()
    {
        EnsureDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "SELECT Name, Code FROM SilverReferees";
        var result = connection.Query<(string Name, string Code)>(sql);

        return result.AsList();
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new char[length];

        for (int i = 0; i < length; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }

        return new string(code);
    }
}

using Microsoft.Data.Sqlite;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class GoldenRefereeService
{
    private const string DbFile = "app.db";

    public static async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
        CREATE TABLE IF NOT EXISTS GoldenReferees (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE,
            Code TEXT NOT NULL
        );";

        await connection.ExecuteAsync(sql);
    }

    public static async Task<string> CreateRefereeAsync(string name)
    {
        await InitializeDatabaseAsync();
        string code = GenerateRandomCode(8);

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "INSERT INTO GoldenReferees (Name, Code) VALUES (@Name, @Code)";
        await connection.ExecuteAsync(sql, new { Name = name, Code = code });

        return code;
    }

    public static async Task<List<GoldenReferee>> GetAllRefereesAsync()
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, Name, Code FROM GoldenReferees";
        var referees = await connection.QueryAsync<GoldenReferee>(sql);
        return referees.ToList();
    }

    public static async Task DeleteRefereeAsync(string name)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "DELETE FROM GoldenReferees WHERE Name = @Name";
        await connection.ExecuteAsync(sql, new { Name = name });
    }

    public static async Task<GoldenReferee?> GetRefereeByCodeAsync(string code)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, Name, Code FROM GoldenReferees WHERE Code = @Code";
        return await connection.QuerySingleOrDefaultAsync<GoldenReferee>(sql, new { Code = code });
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
    public static async Task<int> GetGoldenRefereeCountAsync()
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT COUNT(*) FROM GoldenReferees";
        var count = await connection.ExecuteScalarAsync<int>(sql);
        return count;
    }

}

public class GoldenReferee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}

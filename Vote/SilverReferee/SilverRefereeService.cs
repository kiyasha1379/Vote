using Microsoft.Data.Sqlite;
using Dapper;
using System.Collections.Concurrent;

public static class SilverRefereeService
{
    private const string DbFile = "referees.db";

    private static readonly ConcurrentDictionary<string, object> _locks = new();

    // ایجاد جدول ریفری نقره‌ای
    public static async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
        CREATE TABLE IF NOT EXISTS SilverReferees (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE,
            Code TEXT NOT NULL
        );";

        await connection.ExecuteAsync(sql);
    }

    // اضافه کردن ریفری به صورت thread-safe
    public static async Task<string> CreateRefereeAsync(string name)
    {
        await InitializeDatabaseAsync();
        string code = GenerateRandomCode(8);

        var lockObj = _locks.GetOrAdd(name, _ => new object());
        lock (lockObj)
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            connection.Open();

            var sql = "INSERT INTO SilverReferees (Name, Code) VALUES (@Name, @Code)";
            connection.Execute(sql, new { Name = name, Code = code });
        }

        return code;
    }

    // گرفتن همه ریفری‌ها
    public static async Task<List<SilverReferee>> GetAllRefereesAsync()
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, Name, Code FROM SilverReferees";
        var result = await connection.QueryAsync<SilverReferee>(sql);
        return result.ToList();
    }

    // حذف ریفری
    public static async Task DeleteRefereeAsync(string name)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "DELETE FROM SilverReferees WHERE Name = @Name";
        await connection.ExecuteAsync(sql, new { Name = name });
    }

    // گرفتن ریفری بر اساس کد
    public static async Task<SilverReferee?> GetRefereeByCodeAsync(string code)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, Name, Code FROM SilverReferees WHERE Code = @Code";
        return await connection.QuerySingleOrDefaultAsync<SilverReferee>(sql, new { Code = code });
    }

    // تولید کد تصادفی
    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}

// مدل ریفری نقره‌ای
public class SilverReferee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}

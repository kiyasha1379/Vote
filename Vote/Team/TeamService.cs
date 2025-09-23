using Microsoft.Data.Sqlite;
using Dapper;
using System.Collections.Concurrent;

public static class TeamService
{
    private const string DbFile = "app.db";
    private static readonly object _lockObj = new(); // برای عملیات بحرانی

    // ایجاد جدول تیم/فرد
    public static async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
        CREATE TABLE IF NOT EXISTS Teams (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            GoldenJudgeVotes INTEGER NOT NULL DEFAULT 0,
            SilverJudgeVotes INTEGER NOT NULL DEFAULT 0,
            UserVotes INTEGER NOT NULL DEFAULT 0
        );";

        await connection.ExecuteAsync(sql);
    }

    // اضافه کردن تیم/فرد
    public static async Task AddTeamAsync(string name)
    {
        await InitializeDatabaseAsync();

        lock (_lockObj)
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            connection.Open();
            var sql = "INSERT INTO Teams (Name, GoldenJudgeVotes, SilverJudgeVotes, UserVotes) VALUES (@Name, 0, 0, 0)";
            connection.Execute(sql, new { Name = name });
        }
    }

    // گرفتن همه تیم‌ها
    public static async Task<List<Team>> GetAllTeamsAsync()
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, Name, GoldenJudgeVotes, SilverJudgeVotes, UserVotes FROM Teams";
        var result = await connection.QueryAsync<Team>(sql);
        return result.ToList();
    }

    // حذف تیم/فرد
    public static async Task DeleteTeamAsync(string name)
    {
        await InitializeDatabaseAsync();

        lock (_lockObj)
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            connection.Open();
            var sql = "DELETE FROM Teams WHERE Name = @Name";
            connection.Execute(sql, new { Name = name });
        }
    }
}

// مدل تیم/فرد
public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int GoldenJudgeVotes { get; set; }
    public int SilverJudgeVotes { get; set; }
    public int UserVotes { get; set; }
}

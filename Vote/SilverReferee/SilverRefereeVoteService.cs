using Microsoft.Data.Sqlite;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;

// سرویس مدیریت رأی‌های داور نقره‌ای
public static class SilverRefereeVoteService
{
    private const string DbFile = "app.db";

    // ایجاد جدول برای ثبت رأی داور نقره‌ای
    public static async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
        CREATE TABLE IF NOT EXISTS SilverRefereeVotes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RefereeCode TEXT NOT NULL,
            TeamId INTEGER NOT NULL,
            UNIQUE(RefereeCode, TeamId)
        );";

        await connection.ExecuteAsync(sql);
    }

    // بررسی اینکه آیا داور قبلاً به تیم رأی داده یا نه
    public static async Task<bool> HasVotedAsync(string refereeCode, int teamId)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT COUNT(1) FROM SilverRefereeVotes WHERE RefereeCode = @Code AND TeamId = @TeamId";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Code = refereeCode, TeamId = teamId });

        return count > 0;
    }

    // ثبت رأی جدید برای داور نقره‌ای
    public static async Task RecordVoteAsync(string refereeCode, int teamId)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "INSERT OR IGNORE INTO SilverRefereeVotes (RefereeCode, TeamId) VALUES (@Code, @TeamId)";
        await connection.ExecuteAsync(sql, new { Code = refereeCode, TeamId = teamId });
    }

    // گرفتن همه رأی‌های یک داور نقره‌ای
    public static async Task<List<SilverRefereeVote>> GetVotesByRefereeAsync(string refereeCode)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, RefereeCode, TeamId FROM SilverRefereeVotes WHERE RefereeCode = @Code";
        var result = await connection.QueryAsync<SilverRefereeVote>(sql, new { Code = refereeCode });
        return result.AsList();
    }
}

// مدل رأی داور نقره‌ای
public class SilverRefereeVote
{
    public int Id { get; set; }
    public string RefereeCode { get; set; } = "";
    public int TeamId { get; set; }
}

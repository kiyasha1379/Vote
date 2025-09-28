using Microsoft.Data.Sqlite;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;

// سرویس مدیریت رأی‌های داور نقره‌ای
public static class SilverRefereeVoteService
{
private static readonly string DbFile = Path.Combine("data", "app.db");


    // ایجاد جدول برای ثبت رأی‌ها
    public static async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
        CREATE TABLE IF NOT EXISTS SilverRefereeVotes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RefereeCode TEXT NOT NULL,
            TeamId INTEGER NOT NULL,
            Score INTEGER NOT NULL,
            UNIQUE(RefereeCode, TeamId)
        );";

        await connection.ExecuteAsync(sql);
    }

    // بررسی اینکه آیا داور قبلاً به تیم رأی داده است
    public static async Task<bool> HasVotedAsync(string refereeCode, int teamId)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT COUNT(1) FROM SilverRefereeVotes WHERE RefereeCode = @RefereeCode AND TeamId = @TeamId";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { RefereeCode = refereeCode, TeamId = teamId });
        return count > 0;
    }

    // ثبت رأی داور با امتیاز
    public static async Task RecordVoteAsync(string refereeCode, int teamId, int score)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"INSERT OR IGNORE INTO SilverRefereeVotes (RefereeCode, TeamId, Score) 
                    VALUES (@RefereeCode, @TeamId, @Score)";
        await connection.ExecuteAsync(sql, new { RefereeCode = refereeCode, TeamId = teamId, Score = score });
    }

    // دریافت مجموع امتیازات یک تیم
    public static async Task<int> GetTotalScoreForTeamAsync(int teamId)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT IFNULL(SUM(Score), 0) FROM SilverRefereeVotes WHERE TeamId = @TeamId";
        var total = await connection.ExecuteScalarAsync<int>(sql, new { TeamId = teamId });
        return total;
    }

    // دریافت تمام رأی‌ها (برای گزارش یا دیباگ)
    public static async Task<IEnumerable<SilverRefereeVote>> GetAllVotesAsync()
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, RefereeCode, TeamId, Score FROM SilverRefereeVotes";
        return await connection.QueryAsync<SilverRefereeVote>(sql);
    }
}

public class SilverRefereeVote
{
    public int Id { get; set; }
    public string RefereeCode { get; set; } = "";
    public int TeamId { get; set; }
    public int Score { get; set; }
}
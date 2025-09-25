using Microsoft.Data.Sqlite;
using Dapper;

public static class GoldenRefereeVoteService
{
    private const string DbFile = "app.db";

    // ایجاد جدول برای ثبت رأی‌ها
    public static async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
        CREATE TABLE IF NOT EXISTS GoldenRefereeVotes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RefereeCode TEXT NOT NULL,
            TeamId INTEGER NOT NULL,
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

        var sql = "SELECT COUNT(1) FROM GoldenRefereeVotes WHERE RefereeCode = @RefereeCode AND TeamId = @TeamId";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { RefereeCode = refereeCode, TeamId = teamId });
        return count > 0;
    }

    // ثبت رأی داور
    public static async Task RecordVoteAsync(string refereeCode, int teamId)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "INSERT OR IGNORE INTO GoldenRefereeVotes (RefereeCode, TeamId) VALUES (@RefereeCode, @TeamId)";
        await connection.ExecuteAsync(sql, new { RefereeCode = refereeCode, TeamId = teamId });
    }
}

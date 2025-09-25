using Microsoft.Data.Sqlite;
using Dapper;

public static class UserVoteService
{
    private const string DbFile = "app.db";

    // ایجاد جدول برای ثبت رأی‌ها
    public static async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
        CREATE TABLE IF NOT EXISTS UserVotes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PhoneNumber TEXT NOT NULL,
            TeamId INTEGER NOT NULL,
            UNIQUE(PhoneNumber, TeamId)
        );";

        await connection.ExecuteAsync(sql);
    }

    // بررسی اینکه کاربر قبلاً به تیم رأی داده است
    public static async Task<bool> HasVotedAsync(string phoneNumber, int teamId)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT COUNT(1) FROM UserVotes WHERE PhoneNumber = @PhoneNumber AND TeamId = @TeamId";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { PhoneNumber = phoneNumber, TeamId = teamId });
        return count > 0;
    }

    // ثبت رأی کاربر
    public static async Task RecordVoteAsync(string phoneNumber, int teamId)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "INSERT OR IGNORE INTO UserVotes (PhoneNumber, TeamId) VALUES (@PhoneNumber, @TeamId)";
        await connection.ExecuteAsync(sql, new { PhoneNumber = phoneNumber, TeamId = teamId });
    }
}

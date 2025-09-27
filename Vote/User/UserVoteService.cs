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
            Score INTEGER NOT NULL,
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

    // ثبت رأی کاربر با امتیاز
    public static async Task RecordVoteAsync(string phoneNumber, int teamId, int score)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
            INSERT OR REPLACE INTO UserVotes (PhoneNumber, TeamId, Score)
            VALUES (@PhoneNumber, @TeamId, @Score)";
        await connection.ExecuteAsync(sql, new { PhoneNumber = phoneNumber, TeamId = teamId, Score = score });
    }

    // گرفتن همه رأی‌های یک کاربر
    public static async Task<List<UserVote>> GetVotesByUserAsync(string phoneNumber)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, PhoneNumber, TeamId, Score FROM UserVotes WHERE PhoneNumber = @PhoneNumber";
        var result = await connection.QueryAsync<UserVote>(sql, new { PhoneNumber = phoneNumber });
        return result.AsList();
    }
}

// مدل رأی کاربر
public class UserVote
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = "";
    public int TeamId { get; set; }
    public int Score { get; set; }   // ⭐ امتیاز اضافه شد
}

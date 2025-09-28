using Microsoft.Data.Sqlite;
using Dapper;
using System.Collections.Concurrent;

public static class ChatIdRepository
{
private static readonly string DbFile = Path.Combine("data", "app.db");

    private static readonly ConcurrentDictionary<long, object> _locks = new();

    // ایجاد جدول ChatIds
    public static async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
        CREATE TABLE IF NOT EXISTS ChatIds (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ChatId INTEGER UNIQUE
        );";

        await connection.ExecuteAsync(sql);
    }

    // ذخیره chatId به صورت thread-safe
    public static async Task AddChatIdAsync(long chatId)
    {
        await InitializeDatabaseAsync();

        var lockObj = _locks.GetOrAdd(chatId, _ => new object());
        lock (lockObj)
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            connection.Open();

            var sql = "INSERT OR IGNORE INTO ChatIds (ChatId) VALUES (@ChatId);";
            connection.Execute(sql, new { ChatId = chatId });
        }
    }

    // حذف chatId
    public static async Task RemoveChatIdAsync(long chatId)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "DELETE FROM ChatIds WHERE ChatId = @ChatId;";
        await connection.ExecuteAsync(sql, new { ChatId = chatId });
    }

    // واکشی همه chatId ها
    public static async Task<List<long>> GetAllChatIdsAsync()
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT ChatId FROM ChatIds;";
        var result = await connection.QueryAsync<long>(sql);
        return result.ToList();
    }
}

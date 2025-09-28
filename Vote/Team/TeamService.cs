using Microsoft.Data.Sqlite;
using Dapper;

public static class TeamService
{
private static readonly string DbFile = Path.Combine("data", "app.db");


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

    public static async Task AddTeamAsync(string name)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();
        var sql = "INSERT INTO Teams (Name, GoldenJudgeVotes, SilverJudgeVotes, UserVotes) VALUES (@Name, 0, 0, 0)";
        await connection.ExecuteAsync(sql, new { Name = name });
    }

    public static async Task<List<Team>> GetAllTeamsAsync()
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, Name, GoldenJudgeVotes, SilverJudgeVotes, UserVotes FROM Teams";
        var result = await connection.QueryAsync<Team>(sql);
        return result.ToList();
    }

    public static async Task<Team?> GetTeamByNameAsync(string name)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "SELECT Id, Name, GoldenJudgeVotes, SilverJudgeVotes, UserVotes FROM Teams WHERE Name = @Name LIMIT 1";
        return await connection.QueryFirstOrDefaultAsync<Team>(sql, new { Name = name });
    }

    public static async Task IncreaseGoldenJudgeVoteAsync(int teamId, int value)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "UPDATE Teams SET GoldenJudgeVotes = GoldenJudgeVotes + @Value WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = teamId, Value = value });
    }
    public static async Task IncreaseSilverJudgeVoteAsync(int teamId, int value)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "UPDATE Teams SET SilverJudgeVotes = SilverJudgeVotes + @Value WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = teamId, Value = value });
    }

    public static async Task DeleteTeamAsync(string name)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();
        var sql = "DELETE FROM Teams WHERE Name = @Name";
        await connection.ExecuteAsync(sql, new { Name = name });
    }
    public static async Task IncreaseUserVoteAsync(int teamId, int value)
    {
        await InitializeDatabaseAsync();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = "UPDATE Teams SET UserVotes = UserVotes + @Value WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = teamId, Value = value });
    }

}

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int GoldenJudgeVotes { get; set; }
    public int SilverJudgeVotes { get; set; }
    public int UserVotes { get; set; }
}

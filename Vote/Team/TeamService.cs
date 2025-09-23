using Microsoft.Data.Sqlite;
using Dapper;


public static class TeamService
{
    private const string DbFile = "app.db";

    // ایجاد  تیم/فرد
    public static void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = @"
        CREATE TABLE IF NOT EXISTS Teams (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            GoldenJudgeVotes INTEGER NOT NULL DEFAULT 0,
            SilverJudgeVotes INTEGER NOT NULL DEFAULT 0,
            UserVotes INTEGER NOT NULL DEFAULT 0
        );";

        connection.Execute(sql);
    }

    public static void AddTeam(string name)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "INSERT INTO Teams (Name, GoldenJudgeVotes, SilverJudgeVotes, UserVotes) VALUES (@Name, 0, 0, 0)";
        connection.Execute(sql, new { Name = name });
    }

    public static List<Team> GetAllTeams()
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "SELECT Id, Name, GoldenJudgeVotes, SilverJudgeVotes, UserVotes FROM Teams";
        return connection.Query<Team>(sql).ToList();
    }

    public static void DeleteTeam(string name)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "DELETE FROM Teams WHERE Name = @Name";
        connection.Execute(sql, new { Name = name });
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

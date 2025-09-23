using Microsoft.Data.Sqlite;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

public static class TeamService
{
    private const string DbFile = "app.db";

    // ایجاد جدول تیم/فرد
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

    // افزودن تیم/فرد جدید
    public static void AddTeam(string name)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "INSERT INTO Teams (Name, GoldenJudgeVotes, SilverJudgeVotes, UserVotes) VALUES (@Name, 0, 0, 0)";
        connection.Execute(sql, new { Name = name });
    }

    // دریافت همه تیم‌ها/افراد
    public static List<Team> GetAllTeams()
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "SELECT Id, Name, GoldenJudgeVotes, SilverJudgeVotes, UserVotes FROM Teams";
        return connection.Query<Team>(sql).ToList();
    }

    // حذف تیم/فرد
    public static void DeleteTeam(string name)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "DELETE FROM Teams WHERE Name = @Name";
        connection.Execute(sql, new { Name = name });
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

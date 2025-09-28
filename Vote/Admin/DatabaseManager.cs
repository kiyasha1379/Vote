using Dapper;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Threading.Tasks;

public static class DatabaseManager
{
private static readonly string DbFile = Path.Combine("data", "app.db");
    
    /// <summary>
    /// حذف کامل دیتابیس و ساخت دوباره از صفر
    /// </summary>
    public static async Task ResetDatabaseAsync()
    {
        // اگه فایل دیتابیس وجود داشته باشه، حذفش کن
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        await connection.OpenAsync();

        var sql = @"
        DROP TABLE IF EXISTS Codes;
        DROP TABLE IF EXISTS GoldenReferees;
        DROP TABLE IF EXISTS GoldenRefereeVotes;
        DROP TABLE IF EXISTS SilverReferees;
        DROP TABLE IF EXISTS SilverRefereeVotes;
        DROP TABLE IF EXISTS Teams;
        DROP TABLE IF EXISTS UserVotes;
        DROP TABLE IF EXISTS Users;
        ";

        await connection.ExecuteAsync(sql);

        // ساخت دوباره دیتابیس از صفر
        // await InitializeDatabaseAsync();
    }

    /// <summary>
    /// ایجاد جداول مورد نیاز
    /// </summary>
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
        );

        CREATE TABLE IF NOT EXISTS Codes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Code TEXT NOT NULL,
            IsUsed INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL,
            Phone TEXT
        );
        ";

        await connection.ExecuteAsync(sql);
    }
}

using Microsoft.Data.Sqlite;
using Dapper;


public static class SilverRefereeService
{
    private const string DbFile = "referees.db";

    // ایجاد جدول ریفری نقره‌ای
    public static void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = @"
        CREATE TABLE IF NOT EXISTS SilverReferees (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE,
            Code TEXT NOT NULL
        );";

        connection.Execute(sql);
    }

    // اضافه کردن ریفری
    public static string CreateReferee(string name)
    {
        InitializeDatabase();
        string code = GenerateRandomCode(8);

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "INSERT INTO SilverReferees (Name, Code) VALUES (@Name, @Code)";
        connection.Execute(sql, new { Name = name, Code = code });

        return code; // ← این خط اضافه شده
    }


    // گرفتن همه ریفری‌ها
    public static List<SilverReferee> GetAllReferees()
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "SELECT Id, Name, Code FROM SilverReferees";
        return connection.Query<SilverReferee>(sql).ToList();
    }

    // حذف ریفری
    public static void DeleteReferee(string name)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "DELETE FROM SilverReferees WHERE Name = @Name";
        connection.Execute(sql, new { Name = name });
    }

    // گرفتن ریفری بر اساس کد
    public static SilverReferee? GetRefereeByCode(string code)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = "SELECT Id, Name, Code FROM SilverReferees WHERE Code = @Code";
        return connection.QuerySingleOrDefault<SilverReferee>(sql, new { Code = code });
    }

    // تولید کد تصادفی
    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}

// مدل ریفری نقره‌ای
public class SilverReferee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}

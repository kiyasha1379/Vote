using Microsoft.Data.Sqlite;
using Dapper;

public static class CodeService
{
    private const string DbFile = "app.db";

    public static void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        var sql = @"
        CREATE TABLE IF NOT EXISTS Codes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Code TEXT NOT NULL,
            IsUsed INTEGER NOT NULL DEFAULT 0,
            PhoneNumber TEXT NULL
        );";

        connection.Execute(sql);
    }

    public static void AddCode(string code)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        connection.Execute("INSERT INTO Codes (Code, IsUsed, PhoneNumber) VALUES (@Code, 0, NULL)", new { Code = code });
    }

    public static List<Code> GenerateCodes(int count, int length = 8)
    {
        InitializeDatabase();

        var codes = new List<Code>();
        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        for (int i = 0; i < count; i++)
        {
            var codeStr = GenerateRandomCode(length);
            var code = new Code { CodeValue = codeStr, IsUsed = false, PhoneNumber = null };
            codes.Add(code);

            connection.Execute("INSERT INTO Codes (Code, IsUsed, PhoneNumber) VALUES (@Code, 0, NULL)", new { Code = codeStr });
        }

        return codes;
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static List<Code> GetAllCodes()
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        return connection.Query<Code>("SELECT Id, Code AS CodeValue, IsUsed, PhoneNumber FROM Codes").ToList();
    }

    public static void DeleteCode(string code)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        connection.Execute("DELETE FROM Codes WHERE Code = @Code", new { Code = code });
    }

    public static void AddCodes(IEnumerable<string> codes)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        foreach (var code in codes)
            connection.Execute("INSERT INTO Codes (Code, IsUsed, PhoneNumber) VALUES (@Code, 0, NULL)", new { Code = code });
    }

    public static void ClearCodes()
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        connection.Execute("DELETE FROM Codes");
    }

    public static void MarkCodeAsUsed(string code)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        connection.Execute("UPDATE Codes SET IsUsed = 1 WHERE Code = @Code", new { Code = code });
    }

    public static void SetPhoneNumber(string code, string phone)
    {
        InitializeDatabase();

        using var connection = new SqliteConnection($"Data Source={DbFile}");
        connection.Open();

        connection.Execute("UPDATE Codes SET PhoneNumber = @Phone WHERE Code = @Code", new { Phone = phone, Code = code });
    }
}

public class Code
{
    public int Id { get; set; }
    public string CodeValue { get; set; } = "";
    public bool IsUsed { get; set; }
    public string? PhoneNumber { get; set; } = null;
}

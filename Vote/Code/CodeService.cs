using Microsoft.Data.Sqlite;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class CodeService
{
    private const string DbFile = "app.db";
    private static readonly SemaphoreSlim DbSemaphore = new(1, 1); // برای دسترسی async امن
    private static readonly Random Random = new();

    public static async Task InitializeDatabaseAsync()
    {
        await DbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            await connection.OpenAsync();

            var sql = @"
            CREATE TABLE IF NOT EXISTS Codes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Code TEXT NOT NULL UNIQUE,
                IsUsed INTEGER NOT NULL DEFAULT 0,
                PhoneNumber TEXT NULL
            );";

            await connection.ExecuteAsync(sql);
        }
        finally
        {
            DbSemaphore.Release();
        }
    }

    public static async Task AddCodeAsync(string code)
    {
        await InitializeDatabaseAsync();
        await DbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            await connection.OpenAsync();
            await connection.ExecuteAsync("INSERT INTO Codes (Code, IsUsed, PhoneNumber) VALUES (@Code, 0, NULL)", new { Code = code });
        }
        finally
        {
            DbSemaphore.Release();
        }
    }

    public static async Task<List<Code>> GenerateCodesAsync(int count, int length = 8)
    {
        var codes = new List<Code>();
        await InitializeDatabaseAsync();
        await DbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            await connection.OpenAsync();

            for (int i = 0; i < count; i++)
            {
                string codeStr = GenerateRandomCode(length);
                var code = new Code { CodeValue = codeStr, IsUsed = false, PhoneNumber = null };
                codes.Add(code);

                await connection.ExecuteAsync("INSERT INTO Codes (Code, IsUsed, PhoneNumber) VALUES (@Code, 0, NULL)", new { Code = codeStr });
            }
        }
        finally
        {
            DbSemaphore.Release();
        }

        return codes;
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        lock (Random)
        {
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[Random.Next(chars.Length)]).ToArray());
        }
    }

    public static async Task<List<Code>> GetAllCodesAsync()
    {
        await InitializeDatabaseAsync();
        await DbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            await connection.OpenAsync();
            var result = await connection.QueryAsync<Code>("SELECT Id, Code AS CodeValue, IsUsed, PhoneNumber FROM Codes");
            return result.ToList();
        }
        finally
        {
            DbSemaphore.Release();
        }
    }

    public static async Task DeleteCodeAsync(string code)
    {
        await InitializeDatabaseAsync();
        await DbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            await connection.OpenAsync();
            await connection.ExecuteAsync("DELETE FROM Codes WHERE Code = @Code", new { Code = code });
        }
        finally
        {
            DbSemaphore.Release();
        }
    }

    public static async Task AddCodesAsync(IEnumerable<string> codes)
    {
        await InitializeDatabaseAsync();
        await DbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            await connection.OpenAsync();

            foreach (var code in codes)
                await connection.ExecuteAsync("INSERT INTO Codes (Code, IsUsed, PhoneNumber) VALUES (@Code, 0, NULL)", new { Code = code });
        }
        finally
        {
            DbSemaphore.Release();
        }
    }

    public static async Task ClearCodesAsync()
    {
        await InitializeDatabaseAsync();
        await DbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            await connection.OpenAsync();
            await connection.ExecuteAsync("DELETE FROM Codes");
        }
        finally
        {
            DbSemaphore.Release();
        }
    }

    public static async Task MarkCodeAsUsedAsync(string code)
    {
        await InitializeDatabaseAsync();
        await DbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            await connection.OpenAsync();
            await connection.ExecuteAsync("UPDATE Codes SET IsUsed = 1 WHERE Code = @Code", new { Code = code });
        }
        finally
        {
            DbSemaphore.Release();
        }
    }
    public static async Task<Code?> GetCodeAsync(string codeValue)
    {
        await InitializeDatabaseAsync();
        await DbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            await connection.OpenAsync();

            // فقط یک رکورد خاص را بخوان
            var sql = "SELECT Id, Code AS CodeValue, IsUsed, PhoneNumber FROM Codes WHERE Code = @Code LIMIT 1";
            var code = await connection.QueryFirstOrDefaultAsync<Code>(sql, new { Code = codeValue });

            return code;
        }
        finally
        {
            DbSemaphore.Release();
        }
    }

    public static async Task SetPhoneNumberAsync(string code, string phone)
    {
        await InitializeDatabaseAsync();
        await DbSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={DbFile}");
            await connection.OpenAsync();
            await connection.ExecuteAsync("UPDATE Codes SET PhoneNumber = @Phone WHERE Code = @Code", new { Phone = phone, Code = code });
        }
        finally
        {
            DbSemaphore.Release();
        }
    }
}

public class Code
{
    public int Id { get; set; }
    public string CodeValue { get; set; } = "";
    public bool IsUsed { get; set; }
    public string? PhoneNumber { get; set; } = null;
}

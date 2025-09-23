using Microsoft.Data.Sqlite;

//public static class DbHelper
//{
//    private const string DbFile = "golden_referees.db";

//    public static void Init()
//    {
//        using var connection = new SqliteConnection($"Data Source={DbFile}");
//        connection.Open();

//        var tableCmd = connection.CreateCommand();
//        tableCmd.CommandText = @"
//        CREATE TABLE IF NOT EXISTS GoldenReferees (
//            Id INTEGER PRIMARY KEY AUTOINCREMENT,
//            Name TEXT NOT NULL,
//            Code TEXT NOT NULL
//        );";
//        tableCmd.ExecuteNonQuery();
//    }
//}

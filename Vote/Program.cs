class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Bot is starting...");

        // 👈 اینجا فقط توکن رو بده
        var botHandler = new BotHandler("8274908628:AAEHQELuBPYuxvliXMKXYwr4GX0DXAQ9Eck");

        Console.WriteLine("Bot is running. Press any key to exit.");
        Console.ReadKey();
    }
}

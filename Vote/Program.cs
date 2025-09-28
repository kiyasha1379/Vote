class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Bot is starting...");

        var botHandler = new BotHandler("8274908628:AAEHQELuBPYuxvliXMKXYwr4GX0DXAQ9Eck");

        Console.WriteLine("Bot is running. Press any key to exit.");
        await Task.Delay(-1);
    }
}

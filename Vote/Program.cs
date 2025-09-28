class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Bot is starting...");

        var botHandler = new BotHandler("8289170032:AAEIcEjl7AZmnk1KmYJlWREVrKKlSpQXgo0");

        Console.WriteLine("Bot is running. Press any key to exit.");
        await Task.Delay(-1);
    }
}

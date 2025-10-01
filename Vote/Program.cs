class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Bot is starting...");

        //var botHandler = new BotHandler("8497081800:AAHKeOdBgdSJCjxyfOv0aCJ35_CrYVkY7VY");
        //var botHandler = new BotHandler("8289170032:AAEIcEjl7AZmnk1KmYJlWREVrKKlSpQXgo0");
        var botHandler = new BotHandler("8497081800:AAHKeOdBgdSJCjxyfOv0aCJ35_CrYVkY7VY");

        Console.WriteLine("Bot is running. Press any key to exit.");

        await Task.Delay(-1);
    }
}

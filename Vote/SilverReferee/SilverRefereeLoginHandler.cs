using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

public class SilverRefereeLoginHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;

    public SilverRefereeLoginHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
    }

    // شروع ورود داور نقره‌ای
    public async Task StartLogin(long chatId)
    {
        _userStates[chatId] = "AwaitingSilverRefereeCode";
        await _botClient.SendMessage(chatId, "لطفا کد داور نقره‌ای خود را وارد کنید:");
    }

    // هندل کردن پیام‌ها
    public async Task HandleMessage(long chatId, string text)
    {
        if (_userStates.TryGetValue(chatId, out var state) && state == "AwaitingSilverRefereeCode")
        {
            var code = text.Trim();
            var referee = await SilverRefereeService.GetRefereeByCodeAsync(code); // حتما async و thread-safe باشد

            if (referee != null)
            {
                _userStates[chatId] = "SilverRefereeLoggedIn";

                var buttons = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("دکمه ۱"), new KeyboardButton("دکمه ۲") }
                })
                { ResizeKeyboard = true };

                await _botClient.SendMessage(chatId,
                    $"خوش آمدید {referee.Name}!\nشما وارد پنل داور نقره‌ای شدید.",
                    replyMarkup: buttons);
            }
            else
            {
                await _botClient.SendMessage(chatId, "کد اشتباه است. لطفاً دوباره تلاش کنید:");
            }
        }
    }
}

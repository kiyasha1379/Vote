using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Linq;
using System.Collections.Generic;

public class GoldenRefereeLoginHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<long, string> _userStates;

    public GoldenRefereeLoginHandler(ITelegramBotClient botClient, Dictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
    }

    // شروع فرآیند ورود داور طلایی
    public async Task StartLogin(long chatId)
    {
        _userStates[chatId] = "AwaitingGoldenRefereeCode";
        await _botClient.SendMessage(chatId, "لطفا کد داور طلایی خود را وارد کنید:");
    }

    // هندل کردن پیام‌ها
    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();

        if (_userStates.TryGetValue(chatId, out string state) && state == "AwaitingGoldenRefereeCode")
        {
            var referee = GoldenRefereeService.GetAllReferees()
                            .FirstOrDefault(r => r.Code == text);

            if (referee != default)
            {
                // ورود موفق
                _userStates[chatId] = "GoldenRefereeLoggedIn";
                await _botClient.SendMessage(chatId, $"خوش آمدید {referee.Name}!");

                // نمایش دکمه‌های مخصوص داور طلایی
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("دکمه یک"), new KeyboardButton("دکمه دو") }
                })
                { ResizeKeyboard = true };

                await _botClient.SendMessage(chatId, "گزینه خود را انتخاب کنید:", replyMarkup: keyboard);
            }
            else
            {
                // کد اشتباه
                await _botClient.SendMessage(chatId, "کد وارد شده معتبر نیست. لطفا دوباره تلاش کنید:");
            }
        }
    }
}

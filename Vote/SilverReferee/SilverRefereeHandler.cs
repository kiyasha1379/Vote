using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

public class SilverRefereeHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<long, string> _userStates;
    private readonly AdminHandler _adminHandler;

    private readonly Dictionary<long, bool> _awaitingRefereeName = new();
    private readonly Dictionary<long, bool> _awaitingDeleteRefereeName = new();

    public SilverRefereeHandler(ITelegramBotClient botClient, Dictionary<long, string> userStates, AdminHandler adminHandler)
    {
        _botClient = botClient;
        _userStates = userStates;
        _adminHandler = adminHandler;
    }

    public async Task ShowMenu(long chatId)
    {
        _userStates[chatId] = "SilverRefereeMenu";

        var buttons = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("ایجاد داور نقره‌ای"), new KeyboardButton("حذف داور نقره‌ای") },
            new[] { new KeyboardButton("لیست داور نقره‌ای"), new KeyboardButton("بازگشت") }
        })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(chatId, "منوی داور نقره‌ای:", replyMarkup: buttons);
    }

    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();

        // اگر منتظر نام برای ایجاد هستیم
        if (_awaitingRefereeName.ContainsKey(chatId) && _awaitingRefereeName[chatId])
        {
            string name = text;
            string code = SilverRefereeService.CreateReferee(name);
            await _botClient.SendMessage(chatId, $"داور نقره‌ای ساخته شد!\nنام: {name}\nکد: {code}");

            _awaitingRefereeName.Remove(chatId);
            await _adminHandler.ShowAdminMenu(chatId);
            return;
        }

        // اگر منتظر نام برای حذف هستیم
        if (_awaitingDeleteRefereeName.ContainsKey(chatId) && _awaitingDeleteRefereeName[chatId])
        {
            string name = text;
            SilverRefereeService.DeleteReferee(name);
            await _botClient.SendMessage(chatId, $"داور نقره‌ای با نام '{name}' حذف شد.");

            _awaitingDeleteRefereeName.Remove(chatId);
            await _adminHandler.ShowAdminMenu(chatId);
            return;
        }

        switch (text)
        {
            case "ایجاد داور نقره‌ای":
                _awaitingRefereeName[chatId] = true;
                await _botClient.SendMessage(chatId, "لطفا نام داور نقره‌ای را وارد کنید:");
                break;

            case "حذف داور نقره‌ای":
                _awaitingDeleteRefereeName[chatId] = true;
                await _botClient.SendMessage(chatId, "لطفا نام داور نقره‌ای که می‌خواهید حذف کنید را وارد کنید:");
                break;

            case "لیست داور نقره‌ای":
                var referees = SilverRefereeService.GetAllReferees();
                if (referees.Count == 0)
                    await _botClient.SendMessage(chatId, "هیچ داور نقره‌ای ثبت نشده است.");
                else
                {
                    string message = "لیست داورهای نقره‌ای:\n\n";
                    foreach (var r in referees)
                        message += $"نام: {r.Name} | کد: {r.Code}\n";
                    await _botClient.SendMessage(chatId, message);
                }
                break;

            case "بازگشت":
                await _botClient.SendMessage(chatId, "بازگشت به منوی ادمین...");
                await _adminHandler.ShowAdminMenu(chatId);
                break;

            default:
                await _botClient.SendMessage(chatId, "گزینه نامعتبر است. لطفا یکی از دکمه‌ها را انتخاب کنید.");
                break;
        }
    }
}

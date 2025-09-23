using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

public class GoldenRefereeHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<long, string> _userStates;
    private readonly Dictionary<long, bool> _awaitingDeleteRefereeName = new();

    private readonly AdminHandler _adminHandler;

    // نگه داشتن وضعیت انتظار نام داور
    private readonly Dictionary<long, bool> _awaitingRefereeName = new();

    public GoldenRefereeHandler(ITelegramBotClient botClient, Dictionary<long, string> userStates, AdminHandler adminHandler)
    {
        _botClient = botClient;
        _userStates = userStates;
        _adminHandler = adminHandler;
    }

    public async Task ShowMenu(long chatId)
    {
        _userStates[chatId] = "GoldenRefereeMenu";

        var buttons = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("ایجاد داور طلایی"), new KeyboardButton("حذف داور طلایی") },
            new[] { new KeyboardButton("لیست داور طلایی"), new KeyboardButton("بازگشت") }
        })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(chatId, "منوی داور طلایی:", replyMarkup: buttons);
    }

    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();

        // اگر منتظر نام داور هستیم
        if (_awaitingRefereeName.ContainsKey(chatId) && _awaitingRefereeName[chatId])
        {
            string name = text;
            string code = GoldenRefereeService.CreateReferee(name); // ذخیره در دیتابیس و تولید کد 8 رقمی
            await _botClient.SendMessage(chatId, $"داور طلایی ساخته شد!\nنام: {name}\nکد: {code}");

            _awaitingRefereeName.Remove(chatId); // وضعیت انتظار نام را حذف می‌کنیم
            await _adminHandler.ShowAdminMenu(chatId);
            return;
        }

        // اگر منتظر نام برای حذف داور هستیم
        if (_awaitingDeleteRefereeName.ContainsKey(chatId) && _awaitingDeleteRefereeName[chatId])
        {
            string name = text;
            GoldenRefereeService.DeleteReferee(name);
            await _botClient.SendMessage(chatId, $"داور طلایی با نام '{name}' حذف شد.");
            _awaitingDeleteRefereeName.Remove(chatId);
            await _adminHandler.ShowAdminMenu(chatId);
            return;
        }

        switch (text)
        {
            case "ایجاد داور طلایی":
                _awaitingRefereeName[chatId] = true;
                await _botClient.SendMessage(chatId, "لطفا نام داور طلایی را وارد کنید:");
                break;

            case "حذف داور طلایی":
                _awaitingDeleteRefereeName[chatId] = true;
                await _botClient.SendMessage(chatId, "لطفا نام داور طلایی که می‌خواهید حذف کنید را وارد کنید:");
                break;

            case "لیست داور طلایی":
                var referees = GoldenRefereeService.GetAllReferees();
                if (referees.Count == 0)
                {
                    await _botClient.SendMessage(chatId, "هیچ داور طلایی ثبت نشده است.");
                }
                else
                {
                    string message = "لیست داورهای طلایی:\n\n";
                    foreach (var r in referees)
                    {
                        message += $"نام: {r.Name} | کد: {r.Code}\n";
                    }
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

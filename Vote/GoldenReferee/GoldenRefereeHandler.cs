using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

public class GoldenRefereeHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;
    private readonly AdminHandler _adminHandler;

    private readonly ConcurrentDictionary<long, bool> _awaitingDeleteRefereeName = new();
    private readonly ConcurrentDictionary<long, bool> _awaitingRefereeName = new();

    public GoldenRefereeHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates, AdminHandler adminHandler)
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

        if (_awaitingRefereeName.TryGetValue(chatId, out bool awaiting) && awaiting)
        {
            string name = text;
            string code = await GoldenRefereeService.CreateRefereeAsync(name);
            await _botClient.SendMessage(chatId, $"داور طلایی ساخته شد!\nنام: {name}\nکد: {code}");

            _awaitingRefereeName.TryRemove(chatId, out _);
            await _adminHandler.ShowAdminMenu(chatId);
            return;
        }

        if (_awaitingDeleteRefereeName.TryGetValue(chatId, out bool awaitingDelete) && awaitingDelete)
        {
            string name = text;
            await GoldenRefereeService.DeleteRefereeAsync(name);
            await _botClient.SendMessage(chatId, $"داور طلایی با نام '{name}' حذف شد.");

            _awaitingDeleteRefereeName.TryRemove(chatId, out _);
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
                var referees = await GoldenRefereeService.GetAllRefereesAsync();
                if (referees.Count == 0)
                    await _botClient.SendMessage(chatId, "هیچ داور طلایی ثبت نشده است.");
                else
                {
                    string message = "لیست داورهای طلایی:\n\n";
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

using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;
using System.Threading;

public class SilverRefereeHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;
    private readonly AdminHandler _adminHandler;

    private readonly ConcurrentDictionary<long, bool> _awaitingRefereeName = new();
    private readonly ConcurrentDictionary<long, bool> _awaitingDeleteRefereeName = new();

    // Semaphore برای جلوگیری از همزمانی در ایجاد/حذف داور
    private static readonly SemaphoreSlim _refereeLock = new(1, 1);

    public SilverRefereeHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates, AdminHandler adminHandler)
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

        if (_awaitingRefereeName.TryGetValue(chatId, out var awaitingCreate) && awaitingCreate)
        {
            string name = text;
            await _refereeLock.WaitAsync();
            try
            {
                string code =await SilverRefereeService.CreateRefereeAsync(name);
                await _botClient.SendMessage(chatId, $"داور نقره‌ای ساخته شد!\nنام: {name}\nکد: {code}");
            }
            finally
            {
                _refereeLock.Release();
            }

            _awaitingRefereeName.TryRemove(chatId, out _);
            await _adminHandler.ShowAdminMenu(chatId);
            return;
        }

        if (_awaitingDeleteRefereeName.TryGetValue(chatId, out var awaitingDelete) && awaitingDelete)
        {
            string name = text;
            await _refereeLock.WaitAsync();
            try
            {
               await SilverRefereeService.DeleteRefereeAsync(name);
                await _botClient.SendMessage(chatId, $"داور نقره‌ای با نام '{name}' حذف شد.");
            }
            finally
            {
                _refereeLock.Release();
            }

            _awaitingDeleteRefereeName.TryRemove(chatId, out _);
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
                var referees = await SilverRefereeService.GetAllRefereesAsync();
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

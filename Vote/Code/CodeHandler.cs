using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

public class CodeHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;
    private const string CodesFile = "codes.txt";
    private readonly AdminHandler _adminHandler;

    private static readonly object FileLock = new(); // برای دسترسی امن به فایل
    private static readonly Random Random = new();

    public CodeHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates, AdminHandler adminHandler)
    {
        _botClient = botClient;
        _userStates = userStates;
        _adminHandler = adminHandler;
    }

    public async Task ShowMenu(long chatId)
    {
        _userStates[chatId] = "CodeMenu";

        var buttons = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("ساخت کد"), new KeyboardButton("پاکسازی") },
            new[] { new KeyboardButton("نمایش لیست کدها"), new KeyboardButton("بازگشت") }
        })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(chatId, "منوی ساخت کد:", replyMarkup: buttons);
    }

    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();
        _userStates.TryGetValue(chatId, out string state);

        if (state == "AwaitingCodeCount")
        {
            if (int.TryParse(text, out int count))
            {
                var codes = GenerateCodes(count);
                await CodeService.AddCodesAsync(codes);

                lock (FileLock)
                {
                    SaveCodesToFile(codes);
                }

                await SendCodesFile(chatId);
                await ShowMenu(chatId);
            }
            else
            {
                await _botClient.SendMessage(chatId, "عدد معتبر وارد کنید.");
            }
            return;
        }

        switch (text)
        {
            case "ساخت کد":
                _userStates[chatId] = "AwaitingCodeCount";
                await _botClient.SendMessage(chatId, "لطفا تعداد کدها را وارد کنید:");
                break;

            case "پاکسازی":
                // حذف فایل کدها به صورت synchronous
                if (File.Exists(CodesFile))
                    File.Delete(CodesFile);

                // پاکسازی کدها در دیتابیس به صورت async و thread-safe
                await CodeService.ClearCodesAsync();
                await _botClient.SendMessage(chatId, "همه کدها پاک شدند.");
                await ShowMenu(chatId);
                break;

            case "نمایش لیست کدها":
                await SendCodesFile(chatId);
                break;

            case "بازگشت":
                _userStates[chatId] = "AdminMenu";
                await _botClient.SendMessage(chatId, "بازگشت به منوی ادمین...");
                await _adminHandler.ShowAdminMenu(chatId);
                break;

            default:
                await _botClient.SendMessage(chatId, "گزینه نامعتبر است.");
                break;
        }
    }

    private List<string> GenerateCodes(int count)
    {
        var codes = new List<string>(count);
        for (int i = 0; i < count; i++)
            codes.Add(GenerateRandomCode(8));
        return codes;
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        lock (Random)
        {
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[Random.Next(chars.Length)]).ToArray());
        }
    }

    private void SaveCodesToFile(List<string> codes)
    {
        using var writer = new StreamWriter(CodesFile, true, Encoding.UTF8);
        foreach (var code in codes)
            writer.WriteLine($"{code},False");
    }

    private async Task SendCodesFile(long chatId)
    {
        if (!File.Exists(CodesFile))
        {
            await _botClient.SendMessage(chatId, "فایل کدها پیدا نشد!");
            return;
        }

        await using var fileStream = new FileStream(CodesFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var inputFile = new InputFileStream(fileStream, CodesFile);

        await _botClient.SendDocument(
            chatId: chatId,
            document: inputFile,
            caption: "📂 لیست کدها"
        );
    }
}

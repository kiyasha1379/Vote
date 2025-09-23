using System.IO;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class CodeHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<long, string> _userStates;
    private const string CodesFile = "codes.txt"; // فایل ذخیره کدها

    public CodeHandler(ITelegramBotClient botClient, Dictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
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

        // انتظار برای تعداد کدها
        if (_userStates.ContainsKey(chatId) && _userStates[chatId] == "AwaitingCodeCount")
        {
            if (int.TryParse(text, out int count))
            {
                var codes = GenerateCodes(count);
                SaveCodesToFile(codes);
                await SendCodesFile(chatId);   // 📂 فایل به جای متن ارسال میشه
                await ShowMenu(chatId); // بازگشت به منوی ساخت کد
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
                if (File.Exists(CodesFile))
                    File.Delete(CodesFile);

                await _botClient.SendMessage(chatId, "همه کدها پاک شدند.");
                await ShowMenu(chatId);
                break;

            case "نمایش لیست کدها":
                if (!File.Exists(CodesFile))
                {
                    await _botClient.SendMessage(chatId, "هیچ کدی ساخته نشده است.");
                }
                else
                {
                    await SendCodesFile(chatId);   // 📂 فایل موجود ارسال میشه
                }
                break;

            case "بازگشت":
                _userStates[chatId] = "AdminMenu";
                // ShowAdminMenu باید اینجا صدا زده بشه
                break;

            default:
                await _botClient.SendMessage(chatId, "گزینه نامعتبر است.");
                break;
        }
    }

    // تولید لیست کدها
    private List<string> GenerateCodes(int count)
    {
        var codes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            codes.Add(GenerateRandomCode(8));
        }
        return codes;
    }

    // تولید کد تصادفی 8 کاراکتری (حروف و عدد)
    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var codeChars = new char[length];
        for (int i = 0; i < length; i++)
            codeChars[i] = chars[random.Next(chars.Length)];

        return new string(codeChars);
    }

    // ذخیره کدها در فایل
    private void SaveCodesToFile(List<string> codes)
    {
        using var writer = new StreamWriter(CodesFile, true, Encoding.UTF8); // append
        foreach (var code in codes)
            writer.WriteLine($"{code},False"); // فلگ false
    }

    // ارسال فایل به تلگرام
private async Task SendCodesFile(long chatId)
{
    if (!File.Exists("codes.txt"))
    {
        await _botClient.SendMessage(chatId, "فایل کدها پیدا نشد!");
        return;
    }

    await using var fileStream = new FileStream("codes.txt", FileMode.Open, FileAccess.Read, FileShare.Read);

    var inputFile = new InputFileStream(fileStream, "codes.txt");

    await _botClient.SendDocument(
        chatId: chatId,
        document: inputFile,
        caption: "📂 لیست کدها"
    );
}

}

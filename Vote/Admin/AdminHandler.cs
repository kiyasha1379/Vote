using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

public class AdminHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates; // thread-safe
    private readonly GoldenRefereeHandler _goldenHandler;
    private readonly SilverRefereeHandler _silverHandler;
    private readonly TeamHandler _teamHandler;
    private readonly CodeHandler _codeHandler;

    public AdminHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
        _goldenHandler = new GoldenRefereeHandler(_botClient, _userStates, this);
        _silverHandler = new SilverRefereeHandler(_botClient, _userStates, this);
        _codeHandler = new CodeHandler(_botClient, _userStates, this);
        _teamHandler = new TeamHandler(_botClient, _userStates, this);
    }

    // نمایش منوی اصلی ادمین
    public async Task ShowAdminMenu(long chatId)
    {
        _userStates[chatId] = "AdminMenu";

        var buttons = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("تنظیم داور طلایی"), new KeyboardButton("تنظیم داور نقره‌ای") },
            new[] { new KeyboardButton("تعریف ارسال نوتیف"), new KeyboardButton("ارسال سوال و گزینه") },
            new[] { new KeyboardButton("تنظیم کد"), new KeyboardButton("تنظیم تیم یا فرد") }
        })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(chatId, "منوی ادمین:", replyMarkup: buttons);
    }

    // هندل کردن پیام‌ها در منوی ادمین
    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();
        _userStates.TryGetValue(chatId, out string state);

        switch (state)
        {
            case "GoldenRefereeMenu":
                await _goldenHandler.HandleMessage(chatId, text);
                return;

            case "SilverRefereeMenu":
                await _silverHandler.HandleMessage(chatId, text);
                return;

            case "CodeMenu":
            case "AwaitingCodeCount":
                await _codeHandler.HandleMessage(chatId, text);
                return;

            case "TeamMenu":
            case "AwaitingCreateTeam":
            case "AwaitingDeleteTeam":
                await _teamHandler.HandleMessage(chatId, text);
                return;
        }

        // منوی اصلی ادمین
        switch (text)
        {
            case "تنظیم داور طلایی":
                await _goldenHandler.ShowMenu(chatId);
                break;

            case "تنظیم داور نقره‌ای":
                await _silverHandler.ShowMenu(chatId);
                break;

            case "تعریف ارسال نوتیف":
                await _botClient.SendMessage(chatId, "گزینه 'تعریف ارسال نوتیف' انتخاب شد.");
                break;

            case "ارسال سوال و گزینه":
                await _botClient.SendMessage(chatId, "گزینه 'ارسال سوال و گزینه' انتخاب شد.");
                break;

            case "تنظیم کد":
                await _codeHandler.ShowMenu(chatId);
                break;

            case "تنظیم تیم یا فرد":
                await _teamHandler.ShowMenu(chatId);
                await _botClient.SendMessage(chatId, "گزینه 'تعریف تیم یا فرد' انتخاب شد.");
                break;

            default:
                await _botClient.SendMessage(chatId, "گزینه نامعتبر است. لطفا یکی از دکمه‌ها را انتخاب کنید.");
                break;
        }
    }
}

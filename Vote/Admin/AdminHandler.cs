using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

public class AdminHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates; // وضعیت کاربرها
    private readonly ConcurrentDictionary<long, string> _tempData = new(); // داده‌های موقت ادمین
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
            new[] { new KeyboardButton("تنظیم کد"), new KeyboardButton("تنظیم تیم یا فرد") },
            new[] { new KeyboardButton("شروع رای‌گیری"), new KeyboardButton("توقف رای‌گیری") },
            new[] { new KeyboardButton("خروج") }
        })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(chatId, "منوی ادمین:", replyMarkup: buttons);
    }

    // هندل کردن پیام‌ها در منوی ادمین
    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();
        _userStates.TryGetValue(chatId, out string state);

        // بررسی زیرمنوها
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

            case "تنظیم کد":
                await _codeHandler.ShowMenu(chatId);
                break;

            case "تنظیم تیم یا فرد":
                await _teamHandler.ShowMenu(chatId);
                break;
            case "شروع رای‌گیری":
                VotingStatus.IsVotingActive = true;
                await _botClient.SendMessage(chatId, "✅ رای‌گیری شروع شد.");
                await ShowAdminMenu(chatId);
                break;

            case "توقف رای‌گیری":
                VotingStatus.IsVotingActive = false;
                await _botClient.SendMessage(chatId, "⛔ رای‌گیری متوقف شد.");
                await ShowAdminMenu(chatId);
                break;

            case "خروج":
                // ریست وضعیت و داده‌های موقت
                _userStates[chatId] = "main_menu";
                _tempData.TryRemove(chatId, out _);

                var mainButtons = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("ادمین"), new KeyboardButton("داور طلایی") },
                    new[] { new KeyboardButton("داور نقره‌ای"), new KeyboardButton("کاربر") }
                })
                { ResizeKeyboard = true };

                await _botClient.SendMessage(chatId,
                    "شما از پنل ادمین خارج شدید. لطفاً یکی از گزینه‌ها را انتخاب کنید:",
                    replyMarkup: mainButtons);
                break;

            default:
                await _botClient.SendMessage(chatId,
                    "گزینه نامعتبر است. لطفاً یکی از دکمه‌ها را انتخاب کنید.");
                break;
        }
    }
}

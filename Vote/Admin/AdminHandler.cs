using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;


public class AdminHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<long, string> _userStates;
    private readonly GoldenRefereeHandler _goldenHandler;
    private readonly SilverRefereeHandler _silverHandler;
    private readonly TeamHandler _teamHandler;
    private readonly CodeHandler _codeHandler;

    public AdminHandler(ITelegramBotClient botClient, Dictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
        _goldenHandler = new GoldenRefereeHandler(_botClient, _userStates, this);
        _silverHandler = new SilverRefereeHandler(_botClient, _userStates, this);
        _codeHandler = new CodeHandler(_botClient, _userStates,this);
        _teamHandler = new TeamHandler(_botClient, _userStates,this);
    }

    // نمایش منوی اصلی ادمین
    public async Task ShowAdminMenu(long chatId)
    {
        _userStates[chatId] = "AdminMenu";

        var buttons = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("تنظیم داور طلایی"), new KeyboardButton("تعریف داور نقره‌ای") },
            new[] { new KeyboardButton("تعریف ارسال نوتیف"), new KeyboardButton("ارسال سوال و گزینه") },
            new[] { new KeyboardButton("ساخت کد"), new KeyboardButton("تعریف تیم یا فرد") }
        })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(chatId, "منوی ادمین:", replyMarkup: buttons);
    }

    // هندل کردن پیام‌ها در منوی ادمین
    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();

        // بررسی اگر کاربر در منوی داور طلایی است
        if (_userStates.ContainsKey(chatId) && _userStates[chatId] == "GoldenRefereeMenu")
        {
            await _goldenHandler.HandleMessage(chatId, text);
            return;
        }

        // بررسی اگر کاربر در منوی داور نقره‌ای است
        if (_userStates.ContainsKey(chatId) && _userStates[chatId] == "SilverRefereeMenu")
        {
            await _silverHandler.HandleMessage(chatId, text);
            return;
        }

        // بررسی اگر کاربر در منوی ساخت کد است
        if (_userStates.ContainsKey(chatId) && _userStates[chatId] == "CodeMenu" || _userStates.ContainsKey(chatId) && _userStates[chatId] == "AwaitingCodeCount")
        {
            await _codeHandler.HandleMessage(chatId, text);
            return;
        }

        if (_userStates.ContainsKey(chatId) && _userStates[chatId] == "TeamMenu" || _userStates[chatId] == "AwaitingCreateTeam" || _userStates[chatId] == "AwaitingDeleteTeam")
        {
            await _teamHandler.HandleMessage(chatId, text);
            return;
        }

        // منوی اصلی ادمین
        switch (text)
        {
            case "تنظیم داور طلایی":
                _userStates[chatId] = "GoldenRefereeMenu";
                await _goldenHandler.ShowMenu(chatId);
                break;

            case "تعریف داور نقره‌ای":
                _userStates[chatId] = "SilverRefereeMenu";
                await _silverHandler.ShowMenu(chatId);
                break;

            case "تعریف ارسال نوتیف":
                await _botClient.SendMessage(chatId, "گزینه 'تعریف ارسال نوتیف' انتخاب شد.");
                break;

            case "ارسال سوال و گزینه":
                await _botClient.SendMessage(chatId, "گزینه 'ارسال سوال و گزینه' انتخاب شد.");
                break;

            case "ساخت کد":
                await _codeHandler.ShowMenu(chatId);
                break;

            case "تعریف تیم یا فرد":
                _userStates[chatId] = "TeamMenu";
                await _teamHandler.ShowMenu(chatId);
                await _botClient.SendMessage(chatId, "گزینه 'تعریف تیم یا فرد' انتخاب شد.");
                break;

            default:
                await _botClient.SendMessage(chatId, "گزینه نامعتبر است. لطفا یکی از دکمه‌ها را انتخاب کنید.");
                break;
        }
    }
}

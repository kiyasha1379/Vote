using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


public class BotHandler
{
    private readonly TelegramBotClient _botClient;
    private readonly AdminHandler _adminHandler;
    private readonly GoldenRefereeLoginHandler _goldenLoginHandler;
    private readonly SilverRefereeLoginHandler _silverLoginHandler;
    private readonly UserLoginHandler _userLoginHandler;

    // نگه داشتن وضعیت کاربرها
    private readonly Dictionary<long, string> _userStates = new();
    private readonly Dictionary<long, string> _tempData = new();

    private const string AdminUser = "admin";
    private const string AdminPass = "1234";

    public BotHandler(string token)
    {
        _botClient = new TelegramBotClient(token);
        _adminHandler = new AdminHandler(_botClient, _userStates);
        _goldenLoginHandler = new GoldenRefereeLoginHandler(_botClient, _userStates);
        _silverLoginHandler = new SilverRefereeLoginHandler(_botClient, _userStates);
        _userLoginHandler = new UserLoginHandler(_botClient, _userStates); 


        var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions { AllowedUpdates = new[] { UpdateType.Message } };
        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cts.Token
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;

        var chatId = message.Chat.Id;
        var text = message.Text?.Trim() ?? "";

        _userStates.TryGetValue(chatId, out string state);

        // شروع ربات
        if (text == "/start")
        {
            var buttons = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("ادمین"), new KeyboardButton("داور طلایی") },
                new[] { new KeyboardButton("داور نقره‌ای"), new KeyboardButton("کاربر") }
            })
            { ResizeKeyboard = true };

            await botClient.SendMessage(chatId, "سلام! یکی از گزینه‌ها رو انتخاب کن:", replyMarkup: buttons, cancellationToken: cancellationToken);
            _userStates[chatId] = "main_menu";
            return;
        }

        if (text == "کاربر")
        {
            await _userLoginHandler.StartLogin(chatId);
            return;
        }

        // اگر کاربر در حالت ورود کاربر است
        if (state == "AwaitingUserCode" || state == "UserLoggedIn" || state == "AwaitingUserInfo" || state == "AwaitingUserName" || state == "AwaitingUserPhone")
        {
            await _userLoginHandler.HandleMessage(chatId, text);
            return;
        }

        // ورود ادمین
        if (text == "ادمین")
        {
            await botClient.SendMessage(chatId, "یوزرنیم خود را وارد کنید:", cancellationToken: cancellationToken);
            _userStates[chatId] = "awaiting_admin_username";
            return;
        }

        if (state == "awaiting_admin_username")
        {
            _tempData[chatId] = text;
            await botClient.SendMessage(chatId, "پسورد خود را وارد کنید:", cancellationToken: cancellationToken);
            _userStates[chatId] = "awaiting_admin_password";
            return;
        }

        if (state == "awaiting_admin_password")
        {
            var enteredUser = _tempData.ContainsKey(chatId) ? _tempData[chatId] : "";
            var enteredPass = text;

            if (enteredUser == AdminUser && enteredPass == AdminPass)
            {
                _userStates[chatId] = "AdminMenu";
                _tempData.Remove(chatId);
                await _adminHandler.ShowAdminMenu(chatId);
                return;
            }
            else
            {
                _userStates[chatId] = "main_menu";
                _tempData.Remove(chatId);
                await botClient.SendMessage(chatId, "یوزرنیم یا پسورد اشتباه است!", cancellationToken: cancellationToken);
                return;
            }
        }

        // ورود داور طلایی
        if (text == "داور طلایی")
        {
            await _goldenLoginHandler.StartLogin(chatId);
            return;
        }

        // اگر کاربر در حالت ورود داور طلایی است
        if (state == "AwaitingGoldenRefereeCode" || state == "GoldenRefereeLoggedIn")
        {
            await _goldenLoginHandler.HandleMessage(chatId, text);
            return;
        }

        // ورود داور نقره‌ای
        if (text == "داور نقره‌ای")
        {
            await _silverLoginHandler.StartLogin(chatId);
            return;
        }

        // اگر کاربر در حالت ورود داور نقره‌ای است
        if (state == "AwaitingSilverRefereeCode" || state == "SilverRefereeLoggedIn")
        {
            await _silverLoginHandler.HandleMessage(chatId, text);
            return;
        }

        // اگر کاربر در منوی ادمین یا زیرمنوهای آن است
        if (state == "AdminMenu" || state == "GoldenRefereeMenu" || state == "SilverRefereeMenu" || state == "CodeMenu" || state == "AwaitingCodeCount"
            || state == "TeamMenu" || state == "AwaitingCreateTeam" || state == "AwaitingDeleteTeam")
        {
            await _adminHandler.HandleMessage(chatId, text);
            return;
        }

        // پیش‌فرض
        await botClient.SendMessage(chatId, $"شما نوشتید: {text}", cancellationToken: cancellationToken);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}

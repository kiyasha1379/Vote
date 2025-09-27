using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<long, string> _userStates = new();
    private readonly ConcurrentDictionary<long, string> _tempData = new();
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

        // انتخاب نقش‌ها
        switch (text)
        {
            case "کاربر":
                if (!VotingStatus.IsVotingActive)
                {
                    await _botClient.SendMessage(chatId, "⚠️ رای‌گیری متوقف است. لطفاً بعدا تلاش کنید.");
                    return;
                }
                await _userLoginHandler.StartLogin(chatId);
                return;
            case "داور طلایی":
                if (!VotingStatus.IsVotingActive)
                {
                    await _botClient.SendMessage(chatId, "⚠️ رای‌گیری متوقف است. لطفاً بعدا تلاش کنید.");
                    return;
                }
                await _goldenLoginHandler.StartLogin(chatId);
                return;
            case "داور نقره‌ای":
                if (!VotingStatus.IsVotingActive)
                {
                    await _botClient.SendMessage(chatId, "⚠️ رای‌گیری متوقف است. لطفاً بعدا تلاش کنید.");
                    return;
                }
                await _silverLoginHandler.StartLogin(chatId);
                return;
            case "ادمین":
                await botClient.SendMessage(chatId, "یوزرنیم خود را وارد کنید:", cancellationToken: cancellationToken);
                _userStates[chatId] = "awaiting_admin_username";
                return;
        }

        // ورود کاربر
        if (state is "AwaitingUserCode" or "UserLoggedIn" or 
            "AwaitingUserInfo" or "AwaitingUserName"
            or "AwaitingUserPhone" or "EnteringUserScore")
        {
            if (!VotingStatus.IsVotingActive)
            {
                await _botClient.SendMessage(chatId, "⚠️ رای‌گیری متوقف است. لطفاً بعدا تلاش کنید.");
                return;
            }
            await _userLoginHandler.HandleMessage(chatId, text);
            return;
        }

        // ورود داور طلایی
        if (state is "AwaitingGoldenRefereeCode" or "GoldenRefereeLoggedIn" or
            "SelectingTeam" or "SelectingGoldenTeam" or "AwaitingGoldenRefereeScore")
        {
            if (!VotingStatus.IsVotingActive)
            {
                await _botClient.SendMessage(chatId, "⚠️ رای‌گیری متوقف است. لطفاً بعدا تلاش کنید.");
                return;
            }
            await _goldenLoginHandler.HandleMessage(chatId, text);

            return;
        }

        // ورود داور نقره‌ای
        if (state is "AwaitingSilverRefereeCode" or "SilverRefereeLoggedIn" or
            "SelectingSilverTeam" or "SelectingSilverTeam" or "EnteringSilverScore" or "AwaitingSilverRefereeScore")
        {
            if (!VotingStatus.IsVotingActive)
            {
                await _botClient.SendMessage(chatId, "⚠️ رای‌گیری متوقف است. لطفاً بعدا تلاش کنید.");
                return;
            }
            await _silverLoginHandler.HandleMessage(chatId, text);
            return;
        }

        // ورود ادمین
        if (state == "awaiting_admin_username")
        {
            _tempData[chatId] = text;
            await botClient.SendMessage(chatId, "پسورد خود را وارد کنید:", cancellationToken: cancellationToken);
            _userStates[chatId] = "awaiting_admin_password";
            return;
        }

        if (state == "awaiting_admin_password")
        {
            var enteredUser = _tempData.GetValueOrDefault(chatId, "");
            var enteredPass = text;

            if (enteredUser == AdminUser && enteredPass == AdminPass)
            {
                _userStates[chatId] = "AdminMenu";
                _tempData.TryRemove(chatId, out _);
                await _adminHandler.ShowAdminMenu(chatId);
            }
            else
            {
                _userStates[chatId] = "main_menu";
                _tempData.TryRemove(chatId, out _);
                await botClient.SendMessage(chatId, "یوزرنیم یا پسورد اشتباه است!", cancellationToken: cancellationToken);
            }
            return;
        }

        // منوی ادمین و زیرمنوها
        if (state is "AdminMenu" or "GoldenRefereeMenu" or "SilverRefereeMenu" or "CodeMenu" or "AwaitingCodeCount"
            or "TeamMenu" or "AwaitingCreateTeam" or "AwaitingDeleteTeam")
        {
            await _adminHandler.HandleMessage(chatId, text);
            return;
        }

        await botClient.SendMessage(chatId, $"شما نوشتید: {text}", cancellationToken: cancellationToken);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}

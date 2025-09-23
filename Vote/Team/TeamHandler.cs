using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

public class TeamHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<long, string> _userStates;
    private readonly AdminHandler _adminHandler;
    
    public TeamHandler(ITelegramBotClient botClient, Dictionary<long, string> userStates, AdminHandler adminHandler)
    {
        _botClient = botClient;
        _userStates = userStates;
        _adminHandler = adminHandler;
    }

    // نمایش منوی تیم/فرد
    public async Task ShowMenu(long chatId)
    {
        _userStates[chatId] = "TeamMenu";

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("🆕 ایجاد فرد یا تیم") },
            new[] { new KeyboardButton("📋 نمایش لیست فرد یا تیم") },
            new[] { new KeyboardButton("❌ حذف تیم یا فرد") },
            new[] { new KeyboardButton("🔙 بازگشت به منوی اصلی") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendMessage(
            chatId,
            "منوی مدیریت تیم/فرد:\nیک گزینه انتخاب کنید:",
            replyMarkup: keyboard
        );
    }

    // هندل کردن پیام‌ها در این منو
    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();

        // اگر در حالت انتظار ایجاد تیم هستیم
        if (_userStates.ContainsKey(chatId) && _userStates[chatId] == "AwaitingCreateTeam")
        {
            TeamService.AddTeam(text);
            await _botClient.SendMessage(chatId, $"✅ '{text}' با موفقیت ثبت شد.");
            await ShowMenu(chatId);
            return;
        }

        // اگر در حالت انتظار حذف تیم هستیم
        if (_userStates.ContainsKey(chatId) && _userStates[chatId] == "AwaitingDeleteTeam")
        {
            TeamService.DeleteTeam(text);
            await _botClient.SendMessage(chatId, $"🗑️ '{text}' حذف شد.");
            await ShowMenu(chatId);
            return;
        }

        switch (text)
        {
            case "🆕 ایجاد فرد یا تیم":
                _userStates[chatId] = "AwaitingCreateTeam";
                await _botClient.SendMessage(chatId, "✍️ نام تیم یا فرد را وارد کنید:");
                break;

            case "📋 نمایش لیست فرد یا تیم":
                var teams = TeamService.GetAllTeams();
                if (teams.Count == 0)
                {
                    await _botClient.SendMessage(chatId, "هیچ تیم/فردی ثبت نشده است.");
                }
                else
                {
                    string message = "📋 لیست تیم‌ها و افراد:\n\n";
                    foreach (var t in teams)
                    {
                        message += $"🔹 {t.Name} | طلایی: {t.GoldenJudgeVotes} | نقره‌ای: {t.SilverJudgeVotes} | کاربر: {t.UserVotes}\n";
                    }
                    await _botClient.SendMessage(chatId, message);
                }
                break;

            case "❌ حذف تیم یا فرد":
                _userStates[chatId] = "AwaitingDeleteTeam";
                await _botClient.SendMessage(chatId, "🗑️ نام تیم یا فردی که می‌خواهید حذف کنید را وارد کنید:");
                break;

            case "🔙 بازگشت به منوی اصلی":
                _userStates[chatId] = "AdminMenu";
                await _botClient.SendMessage(chatId, "بازگشت به منوی ادمین...");
                await _adminHandler.ShowAdminMenu(chatId); break;

            default:
                await _botClient.SendMessage(chatId, "⚠️ گزینه نامعتبر است.");
                break;
        }
    }
}

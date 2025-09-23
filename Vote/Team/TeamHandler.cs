using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;
using System.Threading;

public class TeamHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;
    private readonly AdminHandler _adminHandler;

    // Semaphore برای ایجاد/حذف تیم به صورت امن
    private static readonly SemaphoreSlim _teamLock = new(1, 1);

    public TeamHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates, AdminHandler adminHandler)
    {
        _botClient = botClient;
        _userStates = userStates;
        _adminHandler = adminHandler;
    }

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

    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();

        if (_userStates.TryGetValue(chatId, out var state))
        {
            if (state == "AwaitingCreateTeam")
            {
                await _teamLock.WaitAsync();
                try
                {
                  await  TeamService.AddTeamAsync(text);
                }
                finally
                {
                    _teamLock.Release();
                }

                await _botClient.SendMessage(chatId, $"✅ '{text}' با موفقیت ثبت شد.");
                await ShowMenu(chatId);
                return;
            }

            if (state == "AwaitingDeleteTeam")
            {
                await _teamLock.WaitAsync();
                try
                {
                   await TeamService.DeleteTeamAsync(text);
                }
                finally
                {
                    _teamLock.Release();
                }

                await _botClient.SendMessage(chatId, $"🗑️ '{text}' حذف شد.");
                await ShowMenu(chatId);
                return;
            }
        }

        switch (text)
        {
            case "🆕 ایجاد فرد یا تیم":
                _userStates[chatId] = "AwaitingCreateTeam";
                await _botClient.SendMessage(chatId, "✍️ نام تیم یا فرد را وارد کنید:");
                break;

            case "📋 نمایش لیست فرد یا تیم":
                var teams =await TeamService.GetAllTeamsAsync();
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
                await _adminHandler.ShowAdminMenu(chatId);
                break;

            default:
                await _botClient.SendMessage(chatId, "⚠️ گزینه نامعتبر است.");
                break;
        }
    }
}

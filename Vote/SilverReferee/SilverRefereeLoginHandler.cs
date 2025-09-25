using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

public class SilverRefereeLoginHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;

    public SilverRefereeLoginHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
    }

    // شروع ورود داور نقره‌ای
    public async Task StartLogin(long chatId)
    {
        _userStates[chatId] = "AwaitingSilverRefereeCode";
        await _botClient.SendMessage(chatId, "لطفا کد داور نقره‌ای خود را وارد کنید:");
    }

    // هندل کردن پیام‌ها
    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();

        if (_userStates.TryGetValue(chatId, out var state))
        {
            switch (state)
            {
                case "AwaitingSilverRefereeCode":
                    var referee = await SilverRefereeService.GetRefereeByCodeAsync(text);

                    if (referee != null)
                    {
                        _userStates[chatId] = "SilverRefereeLoggedIn";

                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[] { new KeyboardButton("نمایش لیست تیم یا افراد") }
                        })
                        { ResizeKeyboard = true };

                        await _botClient.SendMessage(chatId,
                            $"خوش آمدید {referee.Name}! شما وارد پنل داور نقره‌ای شدید.",
                            replyMarkup: keyboard);
                    }
                    else
                    {
                        await _botClient.SendMessage(chatId, "کد اشتباه است. لطفاً دوباره تلاش کنید:");
                    }
                    break;

                case "SilverRefereeLoggedIn":
                    if (text == "نمایش لیست تیم یا افراد")
                    {
                        var teams = await TeamService.GetAllTeamsAsync();
                        if (teams.Count == 0)
                        {
                            await _botClient.SendMessage(chatId, "هیچ تیمی ثبت نشده است.");
                            return;
                        }

                        var buttons = teams
                            .Select(t => new[] { new KeyboardButton(t.Name) })
                            .ToArray();

                        var teamKeyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

                        _userStates[chatId] = "SelectingSilverTeam";
                        await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboard);
                    }
                    break;

                case "SelectingSilverTeam":
                    var team = await TeamService.GetTeamByNameAsync(text);
                    if (team != null)
                    {
                        await TeamService.IncreaseSilverJudgeVoteAsync(team.Id, 3);
                        await _botClient.SendMessage(chatId, $"رای شما ثبت شد ✅ (۳ امتیاز نقره‌ای به {team.Name} اضافه شد)");

                        // بازگشت به منوی اصلی داور نقره‌ای
                        _userStates[chatId] = "SilverRefereeLoggedIn";
                    }
                    else
                    {
                        await _botClient.SendMessage(chatId, "تیم یافت نشد. لطفا دوباره انتخاب کنید.");
                    }
                    break;
            }
        }
    }
}

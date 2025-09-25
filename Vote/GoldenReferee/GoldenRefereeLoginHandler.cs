using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

public class GoldenRefereeLoginHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;

    public GoldenRefereeLoginHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
    }

    // شروع فرآیند ورود داور طلایی
    public async Task StartLogin(long chatId)
    {
        _userStates[chatId] = "AwaitingGoldenRefereeCode";
        await _botClient.SendMessage(chatId, "لطفا کد داور طلایی خود را وارد کنید:");
    }

    // هندل کردن پیام‌ها
    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();

        if (_userStates.TryGetValue(chatId, out string state))
        {
            switch (state)
            {
                case "AwaitingGoldenRefereeCode":
                    var referees = await GoldenRefereeService.GetAllRefereesAsync();
                    var referee = referees.FirstOrDefault(r => r.Code == text);

                    if (referee != null)
                    {
                        _userStates[chatId] = "GoldenRefereeLoggedIn";
                        await _botClient.SendMessage(chatId, $"خوش آمدید {referee.Name}! 👑");

                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[] { new KeyboardButton("📋 نمایش لیست تیم‌ها") }
                        })
                        { ResizeKeyboard = true };

                        await _botClient.SendMessage(chatId, "گزینه خود را انتخاب کنید:", replyMarkup: keyboard);
                    }
                    else
                    {
                        await _botClient.SendMessage(chatId, "کد وارد شده معتبر نیست. لطفا دوباره تلاش کنید:");
                    }
                    break;

                case "GoldenRefereeLoggedIn":
                    if (text == "📋 نمایش لیست تیم‌ها")
                    {
                        var teams = await TeamService.GetAllTeamsAsync();

                        // ساخت دکمه‌ها بر اساس نام تیم‌ها
                        var buttons = teams
                            .Select(t => new[] { new KeyboardButton(t.Name) })
                            .ToArray();

                        var teamKeyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

                        _userStates[chatId] = "SelectingTeam";
                        await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboard);
                    }
                    break;

                case "SelectingTeam":
                    var team = await TeamService.GetTeamByNameAsync(text);
                    if (team != null)
                    {
                        await TeamService.IncreaseGoldenJudgeVoteAsync(team.Id, 5);
                        await _botClient.SendMessage(chatId, $"✅ رای شما ثبت شد. (۵ امتیاز به تیم {team.Name} اضافه شد)");

                        // بازگشت به منوی اصلی
                        _userStates[chatId] = "GoldenRefereeLoggedIn";

                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[] { new KeyboardButton("📋 نمایش لیست تیم‌ها") }
                        })
                        { ResizeKeyboard = true };

                        await _botClient.SendMessage(chatId, "گزینه خود را انتخاب کنید:", replyMarkup: keyboard);
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

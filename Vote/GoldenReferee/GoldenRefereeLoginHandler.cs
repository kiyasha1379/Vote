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

        if (!_userStates.TryGetValue(chatId, out var state))
            return;

        switch (state)
        {
            // مرحله وارد کردن کد داور طلایی
            case "AwaitingGoldenRefereeCode":
                var referee = await GoldenRefereeService.GetRefereeByCodeAsync(text);

                if (referee != null)
                {
                    _userStates[chatId] = "GoldenRefereeLoggedIn";

                    var keyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[] { new KeyboardButton("📋 نمایش لیست تیم‌ها") }
                    })
                    { ResizeKeyboard = true };

                    await _botClient.SendMessage(chatId,
                        $"خوش آمدید {referee.Name}! 👑 شما وارد پنل داور طلایی شدید.",
                        replyMarkup: keyboard);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "کد وارد شده معتبر نیست. لطفا دوباره تلاش کنید:");
                }
                break;

            // منوی اصلی بعد از ورود
            case "GoldenRefereeLoggedIn":
                if (text == "📋 نمایش لیست تیم‌ها")
                {
                    var teams = await TeamService.GetAllTeamsAsync();
                    if (teams.Count == 0)
                    {
                        await _botClient.SendMessage(chatId, "هیچ تیمی ثبت نشده است.");
                        return;
                    }

                    var buttons = teams.Select(t => new[] { new KeyboardButton(t.Name) }).ToArray();
                    var teamKeyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

                    _userStates[chatId] = $"SelectingGoldenTeam:{text}";
                    await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboard);
                }
                break;

            // مرحله انتخاب تیم برای رأی
            case string s when s.StartsWith("SelectingGoldenTeam:"):
                var refereeCode = s.Split(':')[1];
                var selectedTeam = await TeamService.GetTeamByNameAsync(text);

                if (selectedTeam != null)
                {
                    bool hasVoted = await GoldenRefereeVoteService.HasVotedAsync(refereeCode, selectedTeam.Id);

                    if (hasVoted)
                    {
                        await _botClient.SendMessage(chatId, $"شما قبلاً به {selectedTeam.Name} رأی داده‌اید ❌");
                    }
                    else
                    {
                        await TeamService.IncreaseGoldenJudgeVoteAsync(selectedTeam.Id, 5);
                        await GoldenRefereeVoteService.RecordVoteAsync(refereeCode, selectedTeam.Id);
                        await _botClient.SendMessage(chatId, $"✅ رای شما ثبت شد. (۵ امتیاز به تیم {selectedTeam.Name} اضافه شد)");
                    }

                    // نمایش دوباره لیست تیم‌ها برای رأی‌دهی مجدد
                    var teams = await TeamService.GetAllTeamsAsync();
                    var buttons = teams.Select(t => new[] { new KeyboardButton(t.Name) }).ToArray();
                    var teamKeyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

                    _userStates[chatId] = $"SelectingGoldenTeam:{refereeCode}";
                    await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboard);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "تیم یافت نشد. لطفا دوباره انتخاب کنید.");
                }
                break;
        }
    }
}

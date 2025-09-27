using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

public class GoldenRefereeLoginHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;
    private readonly ConcurrentDictionary<long, string> _goldentatus = new();
    private readonly ConcurrentDictionary<long, int> _seletctedTeam = new();

    public GoldenRefereeLoginHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
    }

    // 🟢 منوی اصلی
    private ReplyKeyboardMarkup GetMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("ادمین"), new KeyboardButton("داور طلایی") },
            new[] { new KeyboardButton("داور نقره‌ای"), new KeyboardButton("کاربر") }
        })
        { ResizeKeyboard = true };
    }

    // 🟢 دکمه بازگشت
    private ReplyKeyboardMarkup AddBackButton(ReplyKeyboardMarkup markup)
    {
        var rows = markup.Keyboard.ToList();
        rows.Add(new[] { new KeyboardButton("بازگشت 🔙") });
        return new ReplyKeyboardMarkup(rows) { ResizeKeyboard = true };
    }

    // 🟢 ریست کردن اطلاعات کاربر
    private void ResetUser(long chatId)
    {
        _userStates.TryRemove(chatId, out _);
        _goldentatus.TryRemove(chatId, out _);
        _seletctedTeam.TryRemove(chatId, out _);
    }

    // شروع فرآیند ورود داور طلایی
    public async Task StartLogin(long chatId)
    {
        _userStates[chatId] = "AwaitingGoldenRefereeCode";
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("بازگشت 🔙") }
        })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(chatId, "لطفا کد داور طلایی خود را وارد کنید:", replyMarkup: keyboard);
    }

    // هندل کردن پیام‌ها
    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();

        if (!_userStates.TryGetValue(chatId, out var state))
            return;

        // 🟢 اگر کاربر بازگشت زد
        if (text == "بازگشت 🔙")
        {
            ResetUser(chatId);
            await _botClient.SendMessage(chatId, "بازگشت به منوی اصلی 👇", replyMarkup: GetMainMenu());
            _userStates[chatId] = "main_menu";
            return;
        }

        switch (state)
        {
            // مرحله وارد کردن کد داور طلایی
            case "AwaitingGoldenRefereeCode":
                var referee = await GoldenRefereeService.GetRefereeByCodeAsync(text);

                if (referee != null)
                {
                    _userStates[chatId] = "GoldenRefereeLoggedIn";
                    _goldentatus.AddOrUpdate(chatId, text, (key, oldValue) => text);

                    var keyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[] { new KeyboardButton("📋 نمایش لیست تیم‌ها") },
                        new[] { new KeyboardButton("بازگشت 🔙") }
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

                    var buttons = teams.Select(t => new[] { new KeyboardButton(t.Name) }).ToList();
                    buttons.Add(new[] { new KeyboardButton("بازگشت 🔙") }); // ➕ دکمه بازگشت
                    var teamKeyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

                    _userStates[chatId] = "SelectingGoldenTeam";
                    await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboard);
                }
                break;

            // مرحله انتخاب تیم برای رأی
            case "SelectingGoldenTeam":
                var selectedTeam = await TeamService.GetTeamByNameAsync(text);

                if (selectedTeam != null)
                {
                    bool hasVoted = await GoldenRefereeVoteService.HasVotedAsync(_goldentatus[chatId], selectedTeam.Id);

                    if (hasVoted)
                    {
                        await _botClient.SendMessage(chatId, $"شما قبلاً به {selectedTeam.Name} رأی داده‌اید ❌");
                    }
                    else
                    {
                        _userStates[chatId] = "AwaitingGoldenRefereeScore";
                        _seletctedTeam.AddOrUpdate(chatId, selectedTeam.Id, (key, oldValue) => selectedTeam.Id);

                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[] { new KeyboardButton("بازگشت 🔙") }
                        })
                        { ResizeKeyboard = true };

                        await _botClient.SendMessage(chatId,
                            $"لطفاً امتیاز خود را به تیم {selectedTeam.Name} وارد کنید (بین 1 تا 100):",
                            replyMarkup: keyboard);
                    }
                }
                else
                {
                    await _botClient.SendMessage(chatId, "تیم یافت نشد. لطفا دوباره انتخاب کنید.");
                }
                break;

            // مرحله ثبت امتیاز
            case "AwaitingGoldenRefereeScore":
                var refereeCode2 = _goldentatus[chatId];
                var teamId2 = _seletctedTeam[chatId];

                if (int.TryParse(text, out int score) && score >= 1 && score <= 100)
                {
                    bool hasVoted = await GoldenRefereeVoteService.HasVotedAsync(refereeCode2, teamId2);

                    if (hasVoted)
                    {
                        await _botClient.SendMessage(chatId, "شما قبلاً برای این تیم رأی داده‌اید ❌");
                    }
                    else
                    {
                        await TeamService.IncreaseGoldenJudgeVoteAsync(teamId2, score);
                        await GoldenRefereeVoteService.RecordVoteAsync(refereeCode2, teamId2, score);

                        await _botClient.SendMessage(chatId, $"✅ رای شما ثبت شد. ({score} امتیاز به تیم اضافه شد)");
                    }

                    var teams = await TeamService.GetAllTeamsAsync();
                    var buttons = teams.Select(t => new[] { new KeyboardButton(t.Name) }).ToList();
                    buttons.Add(new[] { new KeyboardButton("بازگشت 🔙") }); // ➕ بازگشت
                    var teamKeyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

                    _userStates[chatId] = "SelectingGoldenTeam";
                    _seletctedTeam.TryRemove(chatId, out _);
                    await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboard);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "❌ لطفاً یک عدد معتبر بین 1 تا 100 وارد کنید:");
                }
                break;
        }
    }
}

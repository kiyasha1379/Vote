using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

public class SilverRefereeLoginHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;
    private readonly ConcurrentDictionary<long, string> _silverstatus = new();
    private readonly ConcurrentDictionary<long, int> _seletctedSilverTeam = new();

    public SilverRefereeLoginHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates)
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
        _silverstatus.TryRemove(chatId, out _);
        _seletctedSilverTeam.TryRemove(chatId, out _);
    }

    // شروع ورود داور نقره‌ای
    public async Task StartLogin(long chatId)
    {
        _userStates[chatId] = "AwaitingSilverRefereeCode";
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("بازگشت 🔙") }
        })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(chatId, "لطفا کد داور نقره‌ای خود را وارد کنید:", replyMarkup: keyboard);
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
            // مرحله وارد کردن کد داور نقره‌ای
            case "AwaitingSilverRefereeCode":
                var referee = await SilverRefereeService.GetRefereeByCodeAsync(text);

                if (referee != null)
                {
                    _userStates[chatId] = "SilverRefereeLoggedIn";
                    _silverstatus.AddOrUpdate(chatId, text, (key, oldValue) => text);
                    var keyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[] { new KeyboardButton("نمایش لیست تیم یا افراد") },
                        new[] { new KeyboardButton("بازگشت 🔙") }
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

            // منوی اصلی بعد از ورود
            case "SilverRefereeLoggedIn":
                if (text == "نمایش لیست تیم یا افراد")
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

                    _userStates[chatId] = "SelectingSilverTeam";
                    await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboard);
                }
                break;

            // مرحله انتخاب تیم برای رأی
            case "SelectingSilverTeam":
                var selectedTeam = await TeamService.GetTeamByNameAsync(text);

                if (selectedTeam != null)
                {
                    bool hasVoted = await SilverRefereeVoteService.HasVotedAsync(_silverstatus[chatId], selectedTeam.Id);

                    if (hasVoted)
                    {
                        await _botClient.SendMessage(chatId, $"شما قبلاً به {selectedTeam.Name} رأی داده‌اید ❌");
                    }
                    else
                    {
                        _userStates[chatId] = "AwaitingSilverRefereeScore";
                        _seletctedSilverTeam.AddOrUpdate(chatId, selectedTeam.Id, (key, oldValue) => selectedTeam.Id);

                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[] { new KeyboardButton("بازگشت 🔙") }
                        })
                        { ResizeKeyboard = true };

                        await _botClient.SendMessage(chatId,
                            $"لطفاً امتیاز خود را به تیم {selectedTeam.Name} وارد کنید (بین 1 تا 10):",
                            replyMarkup: keyboard);
                    }
                }
                else
                {
                    await _botClient.SendMessage(chatId, "تیم یافت نشد. لطفا دوباره انتخاب کنید.");
                }
                break;

            // مرحله ثبت امتیاز
            case "AwaitingSilverRefereeScore":
                var refereeCode = _silverstatus[chatId];
                var teamId = _seletctedSilverTeam[chatId];
                if (int.TryParse(text, out int score) && score >= 1 && score <= 10)
                {
                    bool hasVoted = await SilverRefereeVoteService.HasVotedAsync(refereeCode, teamId);

                    if (hasVoted)
                    {
                        await _botClient.SendMessage(chatId, "شما قبلاً برای این تیم رأی داده‌اید ❌");
                    }
                    else
                    {
                        await TeamService.IncreaseSilverJudgeVoteAsync(teamId, score);
                        await SilverRefereeVoteService.RecordVoteAsync(refereeCode, teamId, score);

                        await _botClient.SendMessage(chatId, $"✅ رای شما ثبت شد. ({score} امتیاز به تیم اضافه شد)");
                    }

                    var teams = await TeamService.GetAllTeamsAsync();
                    var buttons = teams.Select(t => new[] { new KeyboardButton(t.Name) }).ToList();
                    buttons.Add(new[] { new KeyboardButton("بازگشت 🔙") }); // ➕ بازگشت

                    var teamKeyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

                    _userStates[chatId] = "SelectingSilverTeam";
                    _seletctedSilverTeam.TryRemove(chatId, out _);
                    await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboard);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "❌ لطفاً یک عدد معتبر بین 1 تا 10 وارد کنید:");
                }
                break;
        }
    }
}

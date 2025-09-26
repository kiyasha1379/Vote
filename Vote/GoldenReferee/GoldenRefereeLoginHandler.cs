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

                    _goldentatus.AddOrUpdate(chatId, text, (key, oldValue) => text);


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

                var teams = await TeamService.GetAllTeamsAsync();
                if (teams.Count == 0)
                {
                    await _botClient.SendMessage(chatId, "هیچ تیمی ثبت نشده است.");
                    return;
                }

                var buttons = teams.Select(t => new[] { new KeyboardButton(t.Name) }).ToArray();
                var teamKeyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

                _userStates[chatId] = "SelectingGoldenTeam";
                await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboard);
                break;

            case "SelectingGoldenTeam":
                {
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
                            // تغییر استیت به وارد کردن امتیاز
                            _userStates[chatId] = "AwaitingGoldenRefereeScore";
                            _seletctedTeam.AddOrUpdate(chatId, selectedTeam.Id, (key, oldValue) => selectedTeam.Id);
                            await _botClient.SendMessage(chatId, $"لطفاً امتیاز خود را به تیم {selectedTeam.Name} وارد کنید (بین 1 تا 100):");
                        }
                    }
                    else
                    {
                        await _botClient.SendMessage(chatId, "تیم یافت نشد. لطفا دوباره انتخاب کنید.");
                    }
                }
                break;
            case "AwaitingGoldenRefereeScore":
                {

                    var refereeCode = _goldentatus[chatId];
                    var teamId = _seletctedTeam[chatId];

                    if (int.TryParse(text, out int score) && score >= 1 && score <= 100)
                    {
                        bool hasVoted = await GoldenRefereeVoteService.HasVotedAsync(refereeCode, teamId);

                        if (hasVoted)
                        {
                            await _botClient.SendMessage(chatId, "شما قبلاً برای این تیم رأی داده‌اید ❌");
                        }
                        else
                        {
                            await TeamService.IncreaseGoldenJudgeVoteAsync(teamId, score);
                            await GoldenRefereeVoteService.RecordVoteAsync(refereeCode, teamId, score);

                            await _botClient.SendMessage(chatId, $"✅ رای شما ثبت شد. ({score} امتیاز به تیم اضافه شد)");
                        }

                        // نمایش دوباره لیست تیم‌ها
                        var teamss = await TeamService.GetAllTeamsAsync();
                        var buttonss = teamss.Select(t => new[] { new KeyboardButton(t.Name) }).ToArray();
                        var teamKeyboards = new ReplyKeyboardMarkup(buttonss) { ResizeKeyboard = true };

                        _userStates[chatId] = "SelectingGoldenTeam";
                        _seletctedTeam.TryRemove(chatId, out _);
                        await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboards);
                    }
                    else
                    {
                        await _botClient.SendMessage(chatId, "❌ لطفاً یک عدد معتبر بین 1 تا 100 وارد کنید:");
                    }
                }
                break;
        }
    }
}

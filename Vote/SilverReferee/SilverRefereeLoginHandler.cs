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

        if (!_userStates.TryGetValue(chatId, out var state))
            return;

        switch (state)
        {
            // مرحله وارد کردن کد داور نقره‌ای
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

                    var buttons = teams.Select(t => new[] { new KeyboardButton(t.Name) }).ToArray();
                    var teamKeyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

                    // ذخیره کد داور نقره‌ای در state
                    _userStates[chatId] = $"SelectingSilverTeam:{text}";
                    await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: teamKeyboard);
                }
                break;

            // مرحله انتخاب تیم برای رأی
            case string s when s.StartsWith("SelectingSilverTeam:"):
                var refereeCode = s.Split(':')[1]; // استخراج کد داور نقره‌ای از state
                var selectedTeam = await TeamService.GetTeamByNameAsync(text);

                if (selectedTeam != null)
                {
                    // بررسی رأی قبلی
                    bool hasVoted = await SilverRefereeVoteService.HasVotedAsync(refereeCode, selectedTeam.Id);

                    if (hasVoted)
                    {
                        await _botClient.SendMessage(chatId, $"شما قبلاً به {selectedTeam.Name} رأی داده‌اید ❌");
                    }
                    else
                    {
                        await TeamService.IncreaseSilverJudgeVoteAsync(selectedTeam.Id, 1);
                        await SilverRefereeVoteService.RecordVoteAsync(refereeCode, selectedTeam.Id);
                        await _botClient.SendMessage(chatId, $"رای شما ثبت شد ✅ (1 امتیاز نقره‌ای به {selectedTeam.Name} اضافه شد)");
                    }

                    // نمایش مجدد لیست تیم‌ها برای رأی‌دهی به تیم‌های دیگر
                    var teams = await TeamService.GetAllTeamsAsync();
                    var buttons = teams.Select(t => new[] { new KeyboardButton(t.Name) }).ToArray();
                    var teamKeyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

                    // همچنان در حالت SelectingSilverTeam باقی بماند
                    _userStates[chatId] = $"SelectingSilverTeam:{refereeCode}";
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

using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

public class AdminHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates; // وضعیت کاربرها
    private readonly ConcurrentDictionary<long, string> _tempData = new(); // داده‌های موقت ادمین
    private const string CodesFile = "codes.txt";
    private readonly GoldenRefereeHandler _goldenHandler;
    private readonly SilverRefereeHandler _silverHandler;
    private readonly TeamHandler _teamHandler;
    private readonly CodeHandler _codeHandler;

    public AdminHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
        _goldenHandler = new GoldenRefereeHandler(_botClient, _userStates, this);
        _silverHandler = new SilverRefereeHandler(_botClient, _userStates, this);
        _codeHandler = new CodeHandler(_botClient, _userStates, this);
        _teamHandler = new TeamHandler(_botClient, _userStates, this);
    }

    // نمایش منوی اصلی ادمین
    public async Task ShowAdminMenu(long chatId)
    {
        _userStates[chatId] = "AdminMenu";

        var buttons = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("تنظیم داور طلایی"), new KeyboardButton("تنظیم داور نقره‌ای") },
            new[] { new KeyboardButton("تنظیم کد"), new KeyboardButton("تنظیم تیم یا فرد") },
            new[] { new KeyboardButton("شروع رای‌گیری"), new KeyboardButton("توقف رای‌گیری") },
            new[] { new KeyboardButton("محاسبه و نمایش آرا") },
            new[] { new KeyboardButton("خروج") },
            new[] { new KeyboardButton("پاکسازی تمامی داده‌ها") }
        })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(chatId, "منوی ادمین:", replyMarkup: buttons);
    }

    // هندل کردن پیام‌ها در منوی ادمین
    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();
        _userStates.TryGetValue(chatId, out string state);

        // بررسی زیرمنوها
        switch (state)
        {
            case "GoldenRefereeMenu":
                await _goldenHandler.HandleMessage(chatId, text);
                return;

            case "SilverRefereeMenu":
                await _silverHandler.HandleMessage(chatId, text);
                return;

            case "CodeMenu":
            case "AwaitingCodeCount":
                await _codeHandler.HandleMessage(chatId, text);
                return;

            case "TeamMenu":
            case "AwaitingCreateTeam":
            case "AwaitingDeleteTeam":
                await _teamHandler.HandleMessage(chatId, text);
                return;
        }

        // منوی اصلی ادمین
        switch (text)
        {
            case "تنظیم داور طلایی":
                await _goldenHandler.ShowMenu(chatId);
                break;

            case "تنظیم داور نقره‌ای":
                await _silverHandler.ShowMenu(chatId);
                break;

            case "تنظیم کد":
                await _codeHandler.ShowMenu(chatId);
                break;

            case "تنظیم تیم یا فرد":
                await _teamHandler.ShowMenu(chatId);
                break;
            case "پاکسازی تمامی داده‌ها":
                await DatabaseManager.ResetDatabaseAsync();
                if (File.Exists(CodesFile))
                    File.Delete(CodesFile);
                await ShowAdminMenu(chatId);
                break;
            case "شروع رای‌گیری":
                VotingStatus.IsVotingActive = true;
                await _botClient.SendMessage(chatId, "✅ رای‌گیری شروع شد.");
                await ShowAdminMenu(chatId);
                break;

            case "توقف رای‌گیری":
                VotingStatus.IsVotingActive = false;
                await _botClient.SendMessage(chatId, "⛔ رای‌گیری متوقف شد.");
                await ShowAdminMenu(chatId);
                break;

            case "محاسبه و نمایش آرا":
                await ShowVoteResults(chatId);
                break;

            case "خروج":
                // ریست وضعیت و داده‌های موقت
                _userStates[chatId] = "main_menu";
                _tempData.TryRemove(chatId, out _);

                var mainButtons = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("ادمین"), new KeyboardButton("داور طلایی") },
                    new[] { new KeyboardButton("داور نقره‌ای"), new KeyboardButton("کاربر") }
                })
                { ResizeKeyboard = true };

                await _botClient.SendMessage(chatId,
                    "شما از پنل ادمین خارج شدید. لطفاً یکی از گزینه‌ها را انتخاب کنید:",
                    replyMarkup: mainButtons);
                break;

            default:
                await _botClient.SendMessage(chatId,
                    "گزینه نامعتبر است. لطفاً یکی از دکمه‌ها را انتخاب کنید.");
                break;
        }
    }
    private async Task ShowVoteResults(long chatId)
    {
        var teams = await TeamService.GetAllTeamsAsync();

        // تعداد کل داور طلایی از دیتابیس
        int totalGoldenReferees = await GoldenRefereeService.GetGoldenRefereeCountAsync();
        var silverreferees = await SilverRefereeService.GetAllRefereesAsync();
        int totalSilverReferees = silverreferees.Count;
        int totalUsers = await UserService.GetUsersCountAsync();

        // مجموع رای‌های هر دسته
        int totalGoldenVotes = teams.Sum(t => t.GoldenJudgeVotes);
        int totalSilverVotes = teams.Sum(t => t.SilverJudgeVotes);
        int totalUserVotes = teams.Sum(t => t.UserVotes);

        string message = "📊 نتایج آرا:\n\n";

        foreach (var team in teams)
        {
            double goldenPercent = 0;
            if (totalGoldenReferees > 0)
            {
                // هر داور طلایی 8 امتیاز دارد
                int totalGoldenPoints = totalGoldenReferees * 100;

                // درصد تیم بر اساس امتیاز داورها و 80٪ سهم طلایی
                goldenPercent = Math.Round(((double)team.GoldenJudgeVotes / totalGoldenPoints) * 80, 2);
            }

            double silverPercent = 0;
            if (totalSilverVotes > 0)
            {
                int totalSilverPoints = totalSilverReferees * 100;

                // درصد تیم بر اساس امتیاز داورها و 80٪ سهم طلایی
                silverPercent = Math.Round(((double)team.SilverJudgeVotes / totalSilverPoints) * 10, 2);
            }

            double userPercent = 0;
            if (totalUserVotes > 0)
            {
                int totalUserPoints = totalUsers * 1;

                // درصد تیم بر اساس امتیاز داورها و 80٪ سهم طلایی
                silverPercent = Math.Round(((double)team.UserVotes / totalUserPoints) * 10, 2);
            }

            double totalVots = goldenPercent + silverPercent + userPercent;

            message += $"تیم {team.Name}:\n" +
                       $"   👑 داور طلایی: {goldenPercent}%\n" +
                       $"   🥈 داور نقره‌ای: {silverPercent}%\n" +
                       $"   👤 کاربران: {userPercent}%\n\n" +
                       $"   مجموع آرا: {totalVots}%\n\n";
        }

        await _botClient.SendMessage(chatId, message);
    }


}

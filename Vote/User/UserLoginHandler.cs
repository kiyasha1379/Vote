using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class UserLoginHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;

    private readonly ConcurrentDictionary<long, string> _tempCodes = new();
    private readonly ConcurrentDictionary<long, string> _tempNames = new();
    private readonly ConcurrentDictionary<long, string> _tempPhones = new();
    private readonly ConcurrentDictionary<long, int> _tempTeamId = new();

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _phoneSemaphores = new();

    public UserLoginHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
    }

    public async Task StartLogin(long chatId)
    {
        ResetUser(chatId);
        _userStates[chatId] = "AwaitingUserCode";

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
        new[] { new KeyboardButton("بازگشت 🔙") }
    })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(chatId, "لطفا کد خود را وارد کنید:", replyMarkup: keyboard);
    }

    public async Task HandleMessage(long chatId, string text)
    {
        text = text.Trim();

        // اگر کاربر دکمه بازگشت زد → ریست همه چیز
        if (text == "بازگشت 🔙")
        {
            ResetUser(chatId);

            var buttons = new ReplyKeyboardMarkup(new[]
            {
            new[] { new KeyboardButton("ادمین"), new KeyboardButton("داور طلایی") },
            new[] { new KeyboardButton("داور نقره‌ای"), new KeyboardButton("کاربر") }
             })
            { ResizeKeyboard = true };

            await _botClient.SendMessage(chatId, "بازگشت به منوی اصلی 👇", replyMarkup: buttons);
            _userStates[chatId] = "main_menu";
            return;
        }

        _userStates.TryGetValue(chatId, out string state);

        // مرحله ۱: وارد کردن کد
        if (state == "AwaitingUserCode")
        {
            var codeEntry = await CodeService.GetCodeAsync(text);
            if (codeEntry == null)
            {
                await _botClient.SendMessage(chatId, "❌ کد وارد شده معتبر نیست.");
                return;
            }

            _tempCodes[chatId] = text;
            _userStates[chatId] = "AwaitingUserName";
            await SendWithBack(chatId, "کد معتبر است! لطفا نام خود را وارد کنید:");
            return;
        }

        // مرحله ۲: وارد کردن نام
        if (state == "AwaitingUserName")
        {
            _tempNames[chatId] = text;
            _userStates[chatId] = "AwaitingUserPhone";
            await SendWithBack(chatId, "لطفا شماره تلفن خود را وارد کنید:");
            return;
        }

        // مرحله ۳: وارد کردن شماره تلفن
        if (state == "AwaitingUserPhone")
        {
            string code = _tempCodes[chatId];
            string name = _tempNames[chatId];
            string phone = text;

            var semaphore = _phoneSemaphores.GetOrAdd(phone, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                var codeEntry = await CodeService.GetCodeAsync(code);
                if (codeEntry == null)
                {
                    await _botClient.SendMessage(chatId, "❌ خطای داخلی: کد یافت نشد.");
                    return;
                }

                if (codeEntry.IsUsed && codeEntry.PhoneNumber != phone)
                {
                    await _botClient.SendMessage(chatId, "⚠️ شماره وارد شده با شماره ثبت شده برای این کد متفاوت است!");
                    return;
                }

                if (!codeEntry.IsUsed)
                {
                    await UserService.CreateUserAsync(name, phone, code);
                    await CodeService.MarkCodeAsUsedAsync(code);
                    await CodeService.SetPhoneNumberAsync(code, phone);
                }

                _userStates[chatId] = "UserLoggedIn";
                _tempCodes.TryRemove(chatId, out _);
                _tempNames.TryRemove(chatId, out _);
                _tempPhones[chatId] = phone;

                await ShowTeamList(chatId, phone);
            }
            finally
            {
                semaphore.Release();
            }

            return;
        }

        // مرحله ۴: انتخاب تیم
        if (state == "UserLoggedIn")
        {
            string phone = _tempPhones[chatId];

            var selectedTeam = await TeamService.GetTeamByNameAsync(text);
            if (selectedTeam != null)
            {
                bool hasVoted = await UserVoteService.HasVotedAsync(phone, selectedTeam.Id);
                if (hasVoted)
                {
                    await _botClient.SendMessage(chatId, $"شما قبلاً به {selectedTeam.Name} رأی داده‌اید ❌");
                }
                else
                {
                    _userStates[chatId] = "EnteringUserScore";
                    _tempTeamId[chatId] = selectedTeam.Id;
                    await SendWithBack(chatId, $"لطفاً امتیاز خود را برای تیم {selectedTeam.Name} وارد کنید (عدد بین 1 تا 10):");
                }
            }
            else
            {
                await _botClient.SendMessage(chatId, "تیم یافت نشد. لطفا دوباره انتخاب کنید.");
            }

            return;
        }

        // مرحله ۵: وارد کردن امتیاز
        if (state == "EnteringUserScore")
        {
            string phone = _tempPhones[chatId];
            int teamId = _tempTeamId[chatId];

            if (!int.TryParse(text, out int score) || score < 1 || score > 10)
            {
                await _botClient.SendMessage(chatId, "❌ لطفاً فقط یک عدد بین 1 تا 10 وارد کنید.");
                return;
            }

            await TeamService.IncreaseUserVoteAsync(teamId, score);
            await UserVoteService.RecordVoteAsync(phone, teamId, score);

            await _botClient.SendMessage(chatId, $"✅ رأی شما ثبت شد. ({score} امتیاز به تیم انتخابی اضافه شد)");

            await ShowTeamList(chatId, phone);
        }
    }

    private async Task ShowTeamList(long chatId, string phone)
    {
        var teams = await TeamService.GetAllTeamsAsync();
        if (teams.Count == 0)
        {
            await _botClient.SendMessage(chatId, "هیچ تیمی ثبت نشده است.");
            return;
        }

        var buttons = teams
            .Select(t => new[] { new KeyboardButton(t.Name) })
            .ToList();

        // دکمه بازگشت اضافه می‌کنیم
        buttons.Add(new[] { new KeyboardButton("بازگشت 🔙") });

        var keyboard = new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true };

        _userStates[chatId] = "UserLoggedIn";
        await _botClient.SendMessage(chatId, "لطفا تیم مورد نظر را انتخاب کنید:", replyMarkup: keyboard);
    }

    private async Task SendWithBack(long chatId, string message)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
        new[] { new KeyboardButton("بازگشت 🔙") }
    })
        { ResizeKeyboard = true };


        await _botClient.SendMessage(chatId, message, replyMarkup: keyboard);
    }

    private void ResetUser(long chatId)
    {
        _userStates.TryRemove(chatId, out _);
        _tempCodes.TryRemove(chatId, out _);
        _tempNames.TryRemove(chatId, out _);
        _tempPhones.TryRemove(chatId, out _);
        _tempTeamId.TryRemove(chatId, out _);
    }

}

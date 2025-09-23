using Telegram.Bot;
using System.Collections.Concurrent;

public class UserLoginHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ConcurrentDictionary<long, string> _userStates;
    private readonly ConcurrentDictionary<long, string> _tempCodes = new();
    private readonly ConcurrentDictionary<long, string> _tempNames = new();

    // Semaphore برای هر کد
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _codeSemaphores = new();

    public UserLoginHandler(ITelegramBotClient botClient, ConcurrentDictionary<long, string> userStates)
    {
        _botClient = botClient;
        _userStates = userStates;
    }

    public async Task StartLogin(long chatId)
    {
        _userStates[chatId] = "AwaitingUserCode";
        await _botClient.SendMessage(chatId, "لطفا کد خود را وارد کنید:");
    }

    public async Task HandleMessage(long chatId, string text)
    {
        _userStates.TryGetValue(chatId, out string state);

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
            await _botClient.SendMessage(chatId, "کد معتبر است! لطفا نام خود را وارد کنید:");
            return;
        }

        if (state == "AwaitingUserName")
        {
            _tempNames[chatId] = text;
            _userStates[chatId] = "AwaitingUserPhone";
            await _botClient.SendMessage(chatId, "لطفا شماره تلفن خود را وارد کنید:");
            return;
        }

        if (state == "AwaitingUserPhone")
        {
            string code = _tempCodes[chatId];
            string name = _tempNames[chatId];
            string phone = text;

            var semaphore = _codeSemaphores.GetOrAdd(code, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync();
            try
            {
                var codeEntry = await CodeService.GetCodeAsync(code);

                if (codeEntry == null)
                {
                    await _botClient.SendMessage(chatId, "❌ خطای داخلی: کد یافت نشد.");
                    return;
                }

                if (codeEntry.IsUsed)
                {
                    if (codeEntry.PhoneNumber != phone)
                    {
                        await _botClient.SendMessage(chatId, "⚠️ شماره وارد شده با شماره ثبت شده برای این کد متفاوت است!");
                        return;
                    }
                }
                else
                {
                   await UserService.CreateUserAsync(name, phone, code);
                  await  CodeService.MarkCodeAsUsedAsync(code);
                  await  CodeService.SetPhoneNumberAsync(code, phone);
                }

                await CompleteLogin(chatId, name, phone);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    private async Task CompleteLogin(long chatId, string name, string phone)
    {
        _userStates[chatId] = "UserLoggedIn";
        _tempCodes.TryRemove(chatId, out _);
        _tempNames.TryRemove(chatId, out _);

        await _botClient.SendMessage(chatId, $"🎉 ورود موفق!\n\n👤 نام: {name}\n📞 شماره: {phone}");
    }
}

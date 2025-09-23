using Telegram.Bot;

public class UserLoginHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<long, string> _userStates;

    private readonly Dictionary<long, string> _tempCodes = new();
    private readonly Dictionary<long, string> _tempNames = new();

    public UserLoginHandler(ITelegramBotClient botClient, Dictionary<long, string> userStates)
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

        // مرحله 1: وارد کردن کد
        if (state == "AwaitingUserCode")
        {
            var codeEntry = CodeService.GetAllCodes().FirstOrDefault(c => c.CodeValue == text);

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

        // مرحله 2: وارد کردن نام
        if (state == "AwaitingUserName")
        {
            _tempNames[chatId] = text;
            _userStates[chatId] = "AwaitingUserPhone";

            await _botClient.SendMessage(chatId, "لطفا شماره تلفن خود را وارد کنید:");
            return;
        }

        // مرحله 3: وارد کردن شماره
        if (state == "AwaitingUserPhone")
        {
            string code = _tempCodes[chatId];
            string name = _tempNames[chatId];
            string phone = text;

            var codeEntry = CodeService.GetAllCodes().FirstOrDefault(c => c.CodeValue == code);

            if (codeEntry == null)
            {
                await _botClient.SendMessage(chatId, "❌ خطای داخلی: کد یافت نشد.");
                return;
            }

            if (codeEntry.IsUsed)
            {
                // اگر کد قبلا استفاده شده، شماره باید یکی باشد
                if (codeEntry.PhoneNumber != phone)
                {
                    await _botClient.SendMessage(chatId, "⚠️ شماره وارد شده با شماره ثبت شده برای این کد متفاوت است!");
                    return;
                }
            }
            else
            {
                // اگر کد استفاده نشده، اکانت بساز و شماره ذخیره شود
                UserService.CreateUser(name, phone, code);
                CodeService.MarkCodeAsUsed(code);
                CodeService.SetPhoneNumber(code, phone);
            }

            await CompleteLogin(chatId, name, phone);
        }
    }

    private async Task CompleteLogin(long chatId, string name, string phone)
    {
        _userStates[chatId] = "UserLoggedIn";

        _tempCodes.Remove(chatId);
        _tempNames.Remove(chatId);

        await _botClient.SendMessage(chatId, $"🎉 ورود موفق!\n\n👤 نام: {name}\n📞 شماره: {phone}");
    }
}

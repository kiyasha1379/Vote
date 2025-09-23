using Telegram.Bot;

public class UserLoginHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<long, string> _userStates;

    // دیکشنری‌های جدا برای نگه‌داری موقت
    private readonly Dictionary<long, string> _tempCodes = new();
    private readonly Dictionary<long, string> _tempNames = new();
    private readonly Dictionary<long, string> _tempPhones = new();

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

        // مرحله وارد کردن کد
        if (state == "AwaitingUserCode")
        {
            var allCodes = CodeService.GetAllCodes();
            var codeEntry = allCodes.FirstOrDefault(c => c.CodeValue== text);

            if (codeEntry?.CodeValue == null)
            {
                await _botClient.SendMessage(chatId, "❌ کد وارد شده معتبر نیست.");
                return;
            }

            if (codeEntry.IsUsed)
            {
                await _botClient.SendMessage(chatId, "⚠️ این کد قبلا استفاده شده است.");
                return;
            }

            _tempCodes[chatId] = text;
            _userStates[chatId] = "AwaitingUserName";
            await _botClient.SendMessage(chatId, "✅ کد معتبر است!\nحالا لطفا نام خود را وارد کنید:");
            return;
        }

        // مرحله وارد کردن نام
        if (state == "AwaitingUserName")
        {
            _tempNames[chatId] = text;
            _userStates[chatId] = "AwaitingUserPhone";
            await _botClient.SendMessage(chatId, "لطفا شماره تلفن خود را وارد کنید:");
            return;
        }

        // مرحله وارد کردن شماره
        if (state == "AwaitingUserPhone")
        {
            _tempPhones[chatId] = text;

            string code = _tempCodes[chatId];
            string name = _tempNames[chatId];
            string phone = _tempPhones[chatId];

            // ثبت اطلاعات کاربر
            UserService.CreateUser(name, phone, code);
            CodeService.MarkCodeAsUsed(code);

            _userStates[chatId] = "UserLoggedIn";

            // پاک کردن دیتاهای موقت
            _tempCodes.Remove(chatId);
            _tempNames.Remove(chatId);
            _tempPhones.Remove(chatId);

            await _botClient.SendMessage(chatId, $"🎉 حساب شما ساخته شد!\n\n👤 نام: {name}\n📞 شماره: {phone}");
            return;
        }
    }
}

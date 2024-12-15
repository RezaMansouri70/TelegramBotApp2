using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private static readonly InputPollOption[] PollOptions = new[] { new InputPollOption("سلام"), new InputPollOption("دنیا!") };

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "خطایی در UpdateHandler رخ داده است");
        if (exception is RequestException)
        {
            // تاخیر کوتاه برای جلوگیری از بار اضافی
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("در حال پردازش به‌روزرسانی از نوع: {UpdateType}", update.Type);

        try
        {
            await (update switch
            {
                { Message: { } message } => OnMessage(message),
                { EditedMessage: { } message } => OnMessage(message),
                { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery),
                { InlineQuery: { } inlineQuery } => OnInlineQuery(inlineQuery),
                { ChosenInlineResult: { } chosenInlineResult } => OnChosenInlineResult(chosenInlineResult),
                { Poll: { } poll } => OnPoll(poll),
                { PollAnswer: { } pollAnswer } => OnPollAnswer(pollAnswer),
                _ => UnknownUpdateHandlerAsync(update)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "استثنا در هنگام پردازش به‌روزرسانی");
        }
    }

    private async Task OnMessage(Message msg)
    {
        _logger.LogInformation("پیام دریافتی از نوع: {MessageType}", msg.Type);

        if (msg.Text is not { } messageText)
            return;

        var command = messageText.Split(' ')[0];
        var response = command switch
        {
            "/photo" => SendPhoto(msg),
            "/inline_buttons" => SendInlineKeyboard(msg),
            "/keyboard" => SendReplyKeyboard(msg),
            "/remove" => RemoveKeyboard(msg),
            "/request" => RequestContactAndLocation(msg),
            "/inline_mode" => StartInlineQuery(msg),
            "/poll" => SendPoll(msg),
            "/poll_anonymous" => SendAnonymousPoll(msg),
            "/throw" => FailingHandler(msg),
            _ => Usage(msg)
        };

        var sentMessage = await response;
        _logger.LogInformation("پیام ارسال شد با شناسه: {MessageId}", sentMessage.MessageId);
    }
    async Task<Message> StartInlineQuery(Message msg)
    {
        var button = InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode");
        return await _botClient.SendMessage(msg.Chat, "Press the button to start Inline Query\n\n" +
            "(Make sure you enabled Inline Mode in @BotFather)", replyMarkup: new InlineKeyboardMarkup(button));
    }

    private async Task<Message> Usage(Message msg)
    {
        const string usage = @"
<b><u>منوی دستورات ربات</u></b>:
- /photo          : ارسال یک عکس
- /inline_buttons : ارسال دکمه‌های اینلاین
- /keyboard       : ارسال صفحه‌کلید پاسخ‌گو
- /remove         : حذف صفحه‌کلید
- /request        : درخواست مخاطب یا مکان
- /inline_mode    : شروع کوئری اینلاین
- /poll           : ارسال نظرسنجی
- /poll_anonymous : ارسال نظرسنجی ناشناس
- /throw          : شبیه‌سازی خطا
";
        return await _botClient.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: usage,
            parseMode: ParseMode.Html,
            replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> SendPhoto(Message msg)
    {
        await _botClient.SendChatActionAsync(msg.Chat.Id, ChatAction.UploadPhoto);
        await Task.Delay(2000); // شبیه‌سازی تاخیر
        await using var fileStream = new FileStream("Files/bot.gif", FileMode.Open, FileAccess.Read);
        return await _botClient.SendPhotoAsync(msg.Chat.Id, new InputFileStream(fileStream), caption: "مطالعه کنید https://telegrambots.github.io/book/");
    }

    private async Task<Message> SendInlineKeyboard(Message msg)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("دکمه 1", "callback_data_1"),
                InlineKeyboardButton.WithUrl("بازدید از گیت‌هاب", "https://github.com")
            }
        });

        return await _botClient.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: "یک گزینه انتخاب کنید:",
            replyMarkup: inlineKeyboard);
    }

    private async Task<Message> SendReplyKeyboard(Message msg)
    {
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "گزینه 1", "گزینه 2" },
            new KeyboardButton[] { "گزینه 3", "گزینه 4" }
        })
        {
            ResizeKeyboard = true
        };

        return await _botClient.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: "یک گزینه انتخاب کنید:",
            replyMarkup: replyKeyboard);
    }

    private async Task<Message> RemoveKeyboard(Message msg)
    {
        return await _botClient.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: "صفحه‌کلید حذف شد.",
            replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> RequestContactAndLocation(Message msg)
    {
        var requestKeyboard = new ReplyKeyboardMarkup(new[]
        {
            KeyboardButton.WithRequestContact("اشتراک‌گذاری مخاطب"),
            KeyboardButton.WithRequestLocation("اشتراک‌گذاری مکان")
        })
        {
            ResizeKeyboard = true
        };

        return await _botClient.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: "مخاطب یا مکان خود را به اشتراک بگذارید:",
            replyMarkup: requestKeyboard);
    }

    private async Task<Message> SendPoll(Message msg)
    {
        return await _botClient.SendPollAsync(
            chatId: msg.Chat.Id,
            question: "کدام زبان برنامه‌نویسی را ترجیح می‌دهید؟",
            options: PollOptions.Select(p => new InputPollOption() { Text = p.Text }),
            isAnonymous: false);
    }

    private async Task<Message> SendAnonymousPoll(Message msg)
    {
        return await _botClient.SendPollAsync(
            chatId: msg.Chat.Id,
            question: "کدام زبان برنامه‌نویسی را ترجیح می‌دهید؟",
            options: PollOptions.Select(p => new InputPollOption() { Text = p.Text }));
    }

    private Task<Message> FailingHandler(Message msg)
    {
        throw new NotImplementedException("شبیه‌سازی خطا.");
    }

    private async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        _logger.LogInformation("کوئری بازگشتی دریافت شد: {Data}", callbackQuery.Data);
        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await _botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"شما کلیک کردید: {callbackQuery.Data}");
    }

    private async Task OnInlineQuery(InlineQuery inlineQuery)
    {
        var results = new[]
        {
            new InlineQueryResultArticle(
                id: "1",
                title: "تلگرام.بات",
                inputMessageContent: new InputTextMessageContent("سلام تلگرام!")),
            new InlineQueryResultArticle(
                id: "2",
                title: "فوق‌العاده است",
                inputMessageContent: new InputTextMessageContent("دوباره سلام!"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQuery.Id,
            results,
            cacheTime: 0);
    }

    private async Task OnChosenInlineResult(ChosenInlineResult chosenInlineResult)
    {
        _logger.LogInformation("Chosen inline result: {ResultId}", chosenInlineResult.ResultId);
        await _botClient.SendTextMessageAsync(chosenInlineResult.From.Id, $"You chose: {chosenInlineResult.ResultId}");
    }

    private Task OnPoll(Poll poll)
    {
        _logger.LogInformation("Poll update received: {Question}", poll.Question);
        return Task.CompletedTask;
    }

    private Task OnPollAnswer(PollAnswer pollAnswer)
    {
        var selectedOption = pollAnswer.OptionIds.FirstOrDefault();
        _logger.LogInformation("Poll answer received: {Option}", selectedOption);
        return Task.CompletedTask;
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogWarning("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.LogError("Error {UpdateType}", exception.Message);
        return Task.CompletedTask;
    }
}
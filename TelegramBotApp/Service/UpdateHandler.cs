using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InlineQueryResults;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Polling;
using Microsoft.VisualBasic;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private static readonly InputPollOption[] PollOptions = new[] { new InputPollOption("Hello"), new InputPollOption("World!") };

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error occurred in UpdateHandler");
        if (exception is RequestException)
        {
            // Add a small delay to avoid overloading
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Handling update of type: {UpdateType}", update.Type);

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
            _logger.LogError(ex, "Exception while processing update");
        }
    }

    private async Task OnMessage(Message msg)
    {
        _logger.LogInformation("Received message of type: {MessageType}", msg.Type);

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
        _logger.LogInformation("Message sent with ID: {MessageId}", sentMessage.MessageId);
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
            <b><u>Bot Commands</u></b>:
            - /photo          : Send a photo
            - /inline_buttons : Send inline buttons
            - /keyboard       : Send reply keyboard
            - /remove         : Remove keyboard
            - /request        : Request contact or location
            - /inline_mode    : Start inline query
            - /poll           : Send a poll
            - /poll_anonymous : Send an anonymous poll
            - /throw          : Simulate error
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
        await Task.Delay(2000); // Simulate delay
        await using var fileStream = new FileStream("Files/bot.gif", FileMode.Open, FileAccess.Read);
        return await _botClient.SendPhotoAsync(msg.Chat.Id, new InputFileStream(fileStream), caption: "Read https://telegrambots.github.io/book/");
    }

    private async Task<Message> SendInlineKeyboard(Message msg)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Button 1", "callback_data_1"),
                InlineKeyboardButton.WithUrl("Visit GitHub", "https://github.com")
            }
        });

        return await _botClient.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: "Choose an option:",
            replyMarkup: inlineKeyboard);
    }

    private async Task<Message> SendReplyKeyboard(Message msg)
    {
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Option 1", "Option 2" },
            new KeyboardButton[] { "Option 3", "Option 4" }
        })
        {
            ResizeKeyboard = true
        };

        return await _botClient.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: "Select an option:",
            replyMarkup: replyKeyboard);
    }

    private async Task<Message> RemoveKeyboard(Message msg)
    {
        return await _botClient.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: "Keyboard removed.",
            replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task<Message> RequestContactAndLocation(Message msg)
    {
        var requestKeyboard = new ReplyKeyboardMarkup(new[]
        {
            KeyboardButton.WithRequestContact("Share Contact"),
            KeyboardButton.WithRequestLocation("Share Location")
        })
        {
            ResizeKeyboard = true
        };

        return await _botClient.SendTextMessageAsync(
            chatId: msg.Chat.Id,
            text: "Share your contact or location:",
            replyMarkup: requestKeyboard);
    }

    private async Task<Message> SendPoll(Message msg)
    {
        return await _botClient.SendPollAsync(
            chatId: msg.Chat.Id,
            question: "What is your favorite programming language?",
            options: PollOptions.Select(p => new InputPollOption() { Text = p.Text } ),
            isAnonymous: false);
    }

    private async Task<Message> SendAnonymousPoll(Message msg)
    {
        return await _botClient.SendPollAsync(
            chatId: msg.Chat.Id,
            question: "What is your favorite programming language?",
            options: PollOptions.Select(p => new InputPollOption() { Text = p.Text }));
    }

    private Task<Message> FailingHandler(Message msg)
    {
        throw new NotImplementedException("Simulated failure.");
    }

    private async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        _logger.LogInformation("Callback query received: {Data}", callbackQuery.Data);
        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        await _botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"You clicked: {callbackQuery.Data}");
    }

    private async Task OnInlineQuery(InlineQuery inlineQuery)
    {
        var results = new[]
        {
            new InlineQueryResultArticle(
                id: "1",
                title: "Telegram.Bot",
                inputMessageContent: new InputTextMessageContent("Hello Telegram!")),
            new InlineQueryResultArticle(
                id: "2",
                title: "is awesome",
                inputMessageContent: new InputTextMessageContent("Hello again!"))
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
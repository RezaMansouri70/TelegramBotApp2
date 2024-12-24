using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await (update switch
            {
                { Message: { } message } => OnMessageReceived(message),
                { CallbackQuery: { } callbackQuery } => OnCallbackQueryReceived(callbackQuery),
                _ => HandleUnknownUpdate(update)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در پردازش به‌روزرسانی.");
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "خطا در ربات: {Message}", exception.Message);
        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(Message message)
    {
        _logger.LogInformation("پیام دریافت شد: {MessageType}", message.Type);

        if (message.Text is not { } messageText)
            return;

        var command = messageText.Split(' ')[0];

        switch (command)
        {
            case "/start":
                await SendMainMenu(message);
                break;

            case "/help":
                await SendHelp(message);
                break;
            case "/create_link":
                await GenerateGroupInviteLink(message);
                break;
            default:
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "دستور نامعتبر است. لطفاً از منوی اصلی استفاده کنید.");
                break;
        }
    }
    private async Task GenerateGroupInviteLink(Message message)
    {
        if (message.Chat.Type != ChatType.Group && message.Chat.Type != ChatType.Supergroup)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "این دستور فقط در گروه‌ها قابل استفاده است.");
            return;
        }

        try
        {
            var inviteLink = await _botClient.ExportChatInviteLinkAsync(message.Chat.Id);
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"لینک دعوت گروه:\n{inviteLink}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ایجاد لینک دعوت.");
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "متأسفانه در ایجاد لینک دعوت خطایی رخ داد.");
        }
    }
    private async Task OnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        _logger.LogInformation("درخواست دکمه کلیک شد: {Data}", callbackQuery.Data);

        if (callbackQuery.Data is null)
            return;

        switch (callbackQuery.Data)
        {
            case "view_properties":
                // ارسال پیام متنی
                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: "لیست املاک موجود:\n1. آپارتمان 85 متری - قیمت: 3 میلیارد تومان\n2. ویلا دوبلکس 200 متری - قیمت: 7 میلیارد تومان",
                    replyMarkup: GetMainMenuKeyboard());

                
                   

                // ارسال تصاویر
                var photo1 = "https://arcaonline.ir/vila/2.jpg";
                var photo2 = "https://arcaonline.ir/vila/4.jpg";

                await _botClient.SendPhotoAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    photo: photo1
                );

                await _botClient.SendPhotoAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    photo: photo2
                );
                break;

            case "request_consultation":
                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: "برای مشاوره با کارشناس ما به این آیدی پیام دهید:\n@alireza_ghassmi",
                    replyMarkup: GetMainMenuKeyboard());
                break;

            case "view_villa_photos":
                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: "تصاویر ویلاهای منتخب:");

                var photos = new[]
                {
                    "https://arcaonline.ir/vila/1.jpg",
                    "https://arcaonline.ir/vila/2.jpg",
                    "https://arcaonline.ir/vila/4.jpg",
                    "https://arcaonline.ir/vila/3.jpg"
                };

                foreach (var photo in photos)
                {
                    await _botClient.SendPhotoAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        photo: photo);
                }

                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: "برای بازگشت به منو، گزینه‌ای را انتخاب کنید:",
                    replyMarkup: GetMainMenuKeyboard());
                break;

            case "contact_us":
                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: "تماس با ما:\n📞 09195636195",
                    replyMarkup: GetMainMenuKeyboard());
                break;

            default:
                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: "دستور نامعتبر است.",
                    replyMarkup: GetMainMenuKeyboard());
                break;
        }

        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
    }

    private Task HandleUnknownUpdate(Update update)
    {
        _logger.LogWarning("نوع به‌روزرسانی ناشناخته: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    private async Task SendMainMenu(Message message)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "به املاک ونوس خوش آمدید! لطفاً یکی از گزینه‌های زیر را انتخاب کنید:",
            replyMarkup: GetMainMenuKeyboard());
    }

    private async Task SendHelp(Message message)
    {
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "راهنمای ربات:\n/start - نمایش منوی اصلی\n/help - نمایش راهنما",
            replyMarkup: GetMainMenuKeyboard());
    }

    private InlineKeyboardMarkup GetMainMenuKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("مشاهده لیست املاک", "view_properties"),
            InlineKeyboardButton.WithCallbackData("درخواست مشاوره", "request_consultation")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("تماشای تصاویر ویلاها", "view_villa_photos"),
            InlineKeyboardButton.WithCallbackData("تماس با ما", "contact_us")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("ایجاد لینک دعوت گروه", "create_link")
        }
    });
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

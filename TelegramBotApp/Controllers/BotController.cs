using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<BotController> _logger;

        public BotController(ILogger<BotController> logger)
        {
            // Replace with your actual bot token
            _botClient = new TelegramBotClient("7657833224:AAE8Vn2ds2jhUg7Xyqst-K_rL-YWnuFNJFk");  // Replace with your token
            _logger = logger;
        }
        private void LogAllProperties(object obj, string prefix = "")
        {
            if (obj == null)
            {
                Console.WriteLine($"{prefix}null");
                return;
            }

            var type = obj.GetType();
            Console.WriteLine($"{prefix}{type.Name}:");

            foreach (var property in type.GetProperties())
            {
                try
                {
                    var value = property.GetValue(obj);

                    if (value is string || value == null || property.PropertyType.IsValueType)
                    {
                        Console.WriteLine($"{prefix}  {property.Name}: {value}");
                    }
                    else
                    {
                        // برای آبجکت‌های پیچیده‌تر به صورت بازگشتی عمل می‌کند
                        LogAllProperties(value, prefix + "  ");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{prefix}  {property.Name}: Error reading property - {ex.Message}");
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            LogAllProperties(update);

            _logger.LogInformation("Received message re: {MessageText}", System.Text.Json.JsonSerializer.Serialize(update));

            if (update?.Message != null)
            {
                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text;

                _logger.LogInformation("Received message from chat ID {ChatId}: {MessageText}", chatId, messageText);

                // If /start is sent, show menu with inline buttons
                if (messageText.StartsWith("/start"))
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            new InlineKeyboardButton { Text = "تماس با ما", Url = "https://t.me/alireza_ghassmi" },
                            new InlineKeyboardButton { Text = "لیست املاک", CallbackData = "/list_properties" }
                        },
                        new[]
                        {
                            new InlineKeyboardButton { Text = "مشاوره آنلاین", Url = "https://t.me/alireza_ghassmi" },
                            new InlineKeyboardButton { Text = "درباره‌ی ونوس", CallbackData = "/about_venus" }
                        }
                    });

                    await _botClient.SendTextMessageAsync(chatId, "به بوت املاکی ونوس خوش آمدید! لطفا یکی از گزینه‌ها را انتخاب کنید:", replyMarkup: keyboard);
                    _logger.LogInformation("Sent start message with inline buttons to chat ID {ChatId}", chatId);
                }
            }

            // Handling Callback Queries (When a user clicks an inline button)
            if (update?.CallbackQuery != null)
            {
                var chatId = update.CallbackQuery.Message.Chat.Id;
                var callbackData = update.CallbackQuery.Data;

                _logger.LogInformation("Received callback query with data: {CallbackData}", callbackData);

                // Check if user clicked on "لیست املاک"
                if (callbackData == "/list_properties")
                {
                    // If user clicked "لیست املاک" button
                    await _botClient.SendTextMessageAsync(chatId, "در حال حاضر، ما املاک زیر را داریم:");

                    // Now sending multiple images for villas
                    var mediaGroup = new List<IAlbumInputMedia>
                    {
                        new InputMediaPhoto("https://www.arcaonline.ir/vila/2.jpg"),
                        new InputMediaPhoto("https://www.arcaonline.ir/vila/4.jpg"),
                        new InputMediaPhoto("https://www.arcaonline.ir/vila/3.jpg"),
                        new InputMediaPhoto("https://www.arcaonline.ir/vila/1.jpg")
                    };

                    // Send images as media group (gallery)
                    await _botClient.SendMediaGroupAsync(chatId, mediaGroup);
                    _logger.LogInformation("Sent media group with villa images to chat ID {ChatId}", chatId);
                }
                else if (callbackData == "/about_venus")
                {
                    // Detailed information about Venus Real Estate
                    await _botClient.SendTextMessageAsync(chatId, "ونوس املاک در شهر نور یکی از معتبرترین شرکت‌های املاک است که با هدف فراهم کردن بهترین گزینه‌ها برای خرید، فروش و اجاره ملک فعالیت می‌کند. "
                        + "\n\nویژگی‌های ونوس:\n"
                        + "- مشاوره رایگان و تخصصی در انتخاب ملک\n"
                        + "- انواع ویلا، آپارتمان و زمین\n"
                        + "- تضمین کیفیت و قیمت مناسب\n"
                        + "\nما افتخار داریم که بهترین خدمات را به مشتریان خود ارائه می‌دهیم.\n"
                        + "\nبرای مشاهده جزئیات بیشتر و املاک جدید به وب‌سایت ما مراجعه کنید: https://yourwebsite.com");

                    _logger.LogInformation("Sent information about Venus Real Estate to chat ID {ChatId}", chatId);
                }

                // Optionally acknowledge callback to remove the loading state
                await _botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
                _logger.LogInformation("Acknowledged callback query with ID {CallbackQueryId}", update.CallbackQuery.Id);
            }

            return Ok();
        }
    }
}

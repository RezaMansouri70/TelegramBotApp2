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

        // Initialize bot with your Telegram bot token
        public BotController()
        {
            _botClient = new TelegramBotClient("7657833224:AAE8Vn2ds2jhUg7Xyqst-K_rL-YWnuFNJFk");  // Replace with your token
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update?.Message != null)
            {
                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text; 

                // If /start is sent, show menu with inline buttons
                if (messageText.StartsWith("/start"))
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            new InlineKeyboardButton { Text = "تماس با ما", Url = "https://t.me/alireza_ghassmi" }, // لینک تماس
                            new InlineKeyboardButton { Text = "لیست املاک", CallbackData = "/list_properties" } // برای نمایش لیست املاک
                        },
                        new[]
                        {
                            new InlineKeyboardButton { Text = "مشاوره آنلاین", CallbackData = "/consultation" },
                            new InlineKeyboardButton { Text = "درباره‌ی ونوس", CallbackData = "/about_venus" }
                        }
                    });

                    await _botClient.SendTextMessageAsync(chatId, "به ربات املاکی ونوس خوش آمدید! لطفا یکی از گزینه‌ها را انتخاب کنید:", replyMarkup: keyboard);
                }
                else if (messageText.StartsWith("/list_properties"))
                {
                    // This would ideally list real estate properties
                    await _botClient.SendTextMessageAsync(chatId, "در حال حاضر، ما املاک زیر را داریم:\n\n1. ویلا در نور\n2. خانه ویلایی در نور");
                }
                else if (messageText.StartsWith("/consultation"))
                {
                    await _botClient.SendTextMessageAsync(chatId, "برای مشاوره آنلاین، لطفا با شماره زیر تماس بگیرید:\n\n09195636195");
                }
                else if (messageText.StartsWith("/about_venus"))
                {
                    await _botClient.SendTextMessageAsync(chatId, "ونوس املاک در شهر نور فعالیت می‌کند و به شما کمک می‌کند تا بهترین ملک‌ها را برای خرید و فروش پیدا کنید.");
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "من این پیام را متوجه نشدم. لطفا از گزینه‌های موجود استفاده کنید.");
                }
            }

            return Ok();
        }

        // Endpoint to check the bot's webhook status
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok("Telegram bot is running!");
        }
    }
}

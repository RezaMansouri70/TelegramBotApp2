using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Types;

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

        // The webhook endpoint to receive updates from Telegram
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update.Message != null)
            {
                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text;

                // Respond to different commands
                if (messageText.StartsWith("/start"))
                {
                    await _botClient.SendTextMessageAsync(chatId, "Welcome to the bot! Use /about or /contact for more info.");
                }
                else if (messageText.StartsWith("/about"))
                {
                    await _botClient.SendTextMessageAsync(chatId, "I am a simple bot created to demonstrate Telegram API integration.");
                }
                else if (messageText.StartsWith("/contact"))
                {
                    await _botClient.SendTextMessageAsync(chatId, "You can contact the developer at developer@example.com.");
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "Sorry, I didn't understand that. Try using /start, /about, or /contact.");
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Text;
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
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update, [FromServices] ITelegramBotClient bot, [FromServices] UpdateHandler handleUpdateService, CancellationToken ct)
        {
           
            try
            {
                await handleUpdateService.HandleUpdateAsync(bot, update, ct);
            }
            catch (Exception exception)
            {
                await handleUpdateService.HandleErrorAsync(bot, exception, Telegram.Bot.Polling.HandleErrorSource.HandleUpdateError, ct);
            }
            return Ok();
        }
    }
}

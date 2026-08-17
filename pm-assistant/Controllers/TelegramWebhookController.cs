using Microsoft.AspNetCore.Mvc;
using PmAssistant.Services;
using Telegram.Bot.Types;

namespace PmAssistant.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramWebhookController : ControllerBase
{
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(ILogger<TelegramWebhookController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        // Il webhook Telegram viene gestito dal polling del bot service.
        // Questo endpoint è disponibile per la configurazione webhook se necessario.
        _logger.LogInformation("Telegram webhook ricevuto.");
        return Ok();
    }
}

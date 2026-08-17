using Microsoft.AspNetCore.Mvc;
using PmAssistant.Services;

namespace PmAssistant.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GraphWebhookController : ControllerBase
{
    private readonly ILogger<GraphWebhookController> _logger;

    public GraphWebhookController(ILogger<GraphWebhookController> logger)
    {
        _logger = logger;
    }

    [HttpPost("notifications")]
    public async Task<IActionResult> PostNotification()
    {
        // Il webhook di conferma subscription Graph viene gestito da EmailMonitorService.
        // Questo endpoint riceve le notifiche push per nuove email.
        _logger.LogInformation("Graph webhook notification ricevuto.");
        return Ok();
    }
}

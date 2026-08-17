using Microsoft.AspNetCore.Mvc;
using PmAssistant.Services;
using PmAssistant.Models.Dtos;

namespace PmAssistant.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILlmService _llm;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILlmService llm, ILogger<HealthController> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<HealthStatusResponse>> Get()
    {
        var llmOnline = false;
        try { llmOnline = await _llm.HealthCheckAsync(); }
        catch { /* ignore */ }

        return Ok(new HealthStatusResponse
        {
            LlmOnline = llmOnline,
            LlmModel = "",
            TelegramConnected = true,
            Uptime = DateTime.UtcNow
        });
    }
}

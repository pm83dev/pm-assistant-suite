using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PmAssistant.Services;
using PmAssistant.Services.Chat;
using PmAssistant.Models.Dtos.Chat;

namespace PmAssistant.Controllers.Chat;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Invia un messaggio e ricevi una risposta con tool integration
    /// </summary>
    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatMessageRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _chatService.SendMessageAsync(
                request.Message, 
                request.UserId, 
                request.Tools?.ToList());
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'invio del messaggio");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Recupera la cronologia delle chat per un utente
    /// </summary>
    [HttpGet("history/{userId}")]
    public async Task<ActionResult<List<ChatMessage>>> GetChatHistory(string userId)
    {
        try
        {
            var history = await _chatService.GetChatHistoryAsync(userId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel recupero della cronologia");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Esegue un tool specifico e restituisce il risultato
    /// </summary>
    [HttpPost("execute-tool")]
    public async Task<ActionResult<ChatResponse>> ExecuteTool([FromBody] ToolExecuteRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _chatService.ExecuteToolAsync(
                request.ToolName,
                request.Arguments,
                request.UserId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'esecuzione del tool");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Recupera lo stato attuale dei tool disponibili
    /// </summary>
    [HttpGet("tools/status")]
    public ActionResult<ToolStatusResponse> GetToolStatus()
    {
        var status = _chatService.GetAvailableTools();
        return Ok(status);
    }
}
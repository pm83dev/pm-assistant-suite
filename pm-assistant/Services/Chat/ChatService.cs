using PmAssistant.Services;
using PmAssistant.Models.Dtos.Chat;

namespace PmAssistant.Services.Chat;

public class ChatService : IChatService
{
    private readonly IAssistantAgentService _assistantAgent;
    private readonly ToolDispatcher _toolDispatcher;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IAssistantAgentService assistantAgent,
        ToolDispatcher toolDispatcher,
        ILogger<ChatService> logger)
    {
        _assistantAgent = assistantAgent;
        _toolDispatcher = toolDispatcher;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(string message, string userId, List<string>? toolNames = null)
    {
        try
        {
            // Delega al motore ReAct condiviso con il bot Telegram: l'LLM riceve i tool
            // reali via API e li esegue davvero tramite ToolDispatcher, invece di limitarsi
            // a descriverli come testo nel prompt.
            var replyText = await _assistantAgent.ProcessAsync(message, sessionId: userId);

            return new ChatResponse
            {
                Content = replyText,
                ToolCalls = new List<ToolCall>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'invio del messaggio");
            throw;
        }
    }

    public Task<List<ChatMessage>> GetChatHistoryAsync(string userId)
    {
        // TODO: Implementare una cronologia persistente (memoria o file system)
        return Task.FromResult(new List<ChatMessage>());
    }

    public Dictionary<string, ToolInfo> GetAvailableTools()
    {
        return _toolDispatcher.GetToolDefinitions();
    }

    public async Task<ChatResponse> ExecuteToolAsync(string toolName, string argumentsJson, string userId)
    {
        try
        {
            var result = await _toolDispatcher.ExecuteAsync(toolName, argumentsJson);

            return new ChatResponse
            {
                Content = result,
                ToolCalls = new List<ToolCall>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'esecuzione del tool {ToolName}", toolName);
            throw;
        }
    }
}

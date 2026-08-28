using PmAssistant.Models.Dtos.Chat;

namespace PmAssistant.Services.Chat;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(string message, string userId, List<string>? toolNames = null);
    Task<List<ChatMessage>> GetChatHistoryAsync(string userId);
    Dictionary<string, ToolInfo> GetAvailableTools();
    Task<ChatResponse> ExecuteToolAsync(string toolName, string argumentsJson, string userId);
}
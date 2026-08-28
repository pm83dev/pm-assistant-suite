using System.Text;
using Microsoft.Extensions.Options;
using PmAssistant.Services;
using PmAssistant.Models.Dtos.Chat;

namespace PmAssistant.Services.Chat;

public class ChatService : IChatService
{
    private readonly ILlmService _llmService;
    private readonly ToolDispatcher _toolDispatcher;
    private readonly ILogger<ChatService> _logger;
    private readonly ChatSettings _settings;

    public ChatService(
        ILlmService llmService, 
        ToolDispatcher toolDispatcher,
        IOptions<ChatSettings> settings,
        ILogger<ChatService> logger)
    {
        _llmService = llmService;
        _toolDispatcher = toolDispatcher;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(string message, string userId, List<string>? toolNames = null)
    {
        try
        {
            // Recupera la cronologia dalla cache (implementeremo una semplice cache)
            var history = await GetChatHistoryAsync(userId);
            
            // Prepara il contesto con la cronologia
            var context = new ChatContext
            {
                Messages = history.Concat(new[] { new ChatMessage { Role = "user", Content = message } }).ToList(),
                AvailableTools = GetAvailableTools()
            };

            // Invia al LLM
            var llmResponse = await _llmService.GenerateAsync(
                prompt: BuildPrompt(context),
                systemPrompt: _settings.SystemPrompt,
                maxTokens: _settings.MaxTokens);

            // Analizza la risposta per vedere se richiede tool
            var response = ParseResponse(llmResponse, toolNames);
            
            // Salva nella cronologia
            await SaveMessageAsync(userId, new ChatMessage 
            { 
                Role = "assistant", 
                Content = response.Content,
                ToolCalls = response.ToolCalls
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'invio del messaggio");
            throw;
        }
    }

    public async Task<List<ChatMessage>> GetChatHistoryAsync(string userId)
    {
        // TODO: Implementare una cache semplice (memoria o file system)
        // Per ora restituiamo una lista vuota come placeholder
        return new List<ChatMessage>();
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

            var response = new ChatResponse
            {
                Content = result,
                ToolCalls = new List<ToolCall>()
            };

            await SaveMessageAsync(userId, new ChatMessage
            {
                Role = "assistant",
                Content = response.Content
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'esecuzione del tool {ToolName}", toolName);
            throw;
        }
    }

    private string BuildPrompt(ChatContext context)
    {
        var prompt = new StringBuilder();
        
        // Aggiungi il sistema prompt se configurato
        if (!string.IsNullOrEmpty(_settings.SystemPrompt))
        {
            prompt.AppendLine($"System: {_settings.SystemPrompt}");
            prompt.AppendLine();
        }

        // Aggiungi i messaggi della cronologia
        foreach (var message in context.Messages)
        {
            prompt.AppendLine($"{message.Role}: {message.Content}");
        }

        // Aggiungi informazioni sui tool disponibili
        if (context.AvailableTools.Any())
        {
            prompt.AppendLine("\nTools available:");
            foreach (var tool in context.AvailableTools)
            {
                prompt.AppendLine($"- {tool.Key}: {tool.Value.Description}");
            }
        }

        return prompt.ToString();
    }

    private ChatResponse ParseResponse(string llmResponse, List<string>? toolNames = null)
    {
        // TODO: Implementare il parsing della risposta LLM
        // Per ora restituiamo una risposta semplice
        return new ChatResponse
        {
            Content = llmResponse,
            ToolCalls = new List<ToolCall>()
        };
    }

    private async Task SaveMessageAsync(string userId, ChatMessage message)
    {
        // TODO: Implementare il salvataggio nella cronologia
        // Per ora non facciamo nulla
    }
}

// Models interni per il service
internal class ChatContext
{
    public List<ChatMessage> Messages { get; set; } = new();
    public Dictionary<string, ToolInfo> AvailableTools { get; set; } = new();
}

public class ChatSettings
{
    public string SystemPrompt { get; set; } = "Sei un assistente AI utile per il project management.";
    public int MaxTokens { get; set; } = 4096;
}
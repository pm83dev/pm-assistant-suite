using System.ComponentModel.DataAnnotations;

namespace PmAssistant.Models.Dtos.Chat;

public class ChatMessageRequest
{
    [Required(ErrorMessage = "Il messaggio è obbligatorio")]
    [StringLength(10000, ErrorMessage = "Il messaggio non può superare 10000 caratteri")]
    public string Message { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'ID utente è obbligatorio")]
    public string UserId { get; set; } = string.Empty;

    public List<string>? Tools { get; set; }
}

public class ToolExecuteRequest
{
    [Required(ErrorMessage = "Il nome del tool è obbligatorio")]
    public string ToolName { get; set; } = string.Empty;

    public string Arguments { get; set; } = "{}";

    [Required(ErrorMessage = "L'ID utente è obbligatorio")]
    public string UserId { get; set; } = string.Empty;
}

public class ToolStatusResponse
{
    public Dictionary<string, ToolInfo> AvailableTools { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public string? Error { get; set; }
}
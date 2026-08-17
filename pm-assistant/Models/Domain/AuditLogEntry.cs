namespace PmAssistant.Models.Domain;

public class AuditLogEntry
{
    public string Id { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
    public string? UserId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}

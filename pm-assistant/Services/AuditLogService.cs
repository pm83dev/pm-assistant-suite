using PmAssistant.Services;

namespace PmAssistant.Services;

public interface IAuditLogService
{
    Task LogAsync(string action, string details, string? userId = null, string? entityType = null, string? entityId = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly IGoogleSheetsService _sheets;

    public AuditLogService(IGoogleSheetsService sheets)
    {
        _sheets = sheets;
    }

    public async Task LogAsync(string action, string details, string? userId = null, string? entityType = null, string? entityId = null)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        await _sheets.AppendRowsAsync(
            "AuditLog",
            id,
            DateTime.UtcNow.ToString("o"),
            action,
            details,
            userId ?? "",
            entityType ?? "",
            entityId ?? "");
    }
}

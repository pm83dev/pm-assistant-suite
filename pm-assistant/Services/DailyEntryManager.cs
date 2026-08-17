using PmAssistant.Models.Domain;
using PmAssistant.Services;

namespace PmAssistant.Services;

public interface IDailyEntryManager
{
    Task<DailyLogEntry> CreateEntryAsync(DailyLogEntry entry);
    Task<List<DailyLogEntry>> GetEntriesByDateAsync(DateTime date);
    Task<List<DailyLogEntry>> GetEntriesByMonthAsync(int year, int month);
    Task<ClientInfo?> GetOrCreateClientAsync(string clientName);
}

public class DailyEntryManager : IDailyEntryManager
{
    private readonly IGoogleSheetsService _sheets;
    private readonly IAuditLogService _audit;

    public DailyEntryManager(IGoogleSheetsService sheets, IAuditLogService audit)
    {
        _sheets = sheets;
        _audit = audit;
    }

    public async Task<DailyLogEntry> CreateEntryAsync(DailyLogEntry entry)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        entry.Id = id;
        entry.CreatedAt = DateTime.UtcNow;

        await _sheets.AppendRowsAsync(
            "DailyLogs",
            entry.Id,
            entry.Date.ToString("yyyy-MM-dd"),
            entry.Client,
            entry.Project,
            entry.Hours,
            entry.Description,
            entry.CreatedAt.ToString("o"));

        await _audit.LogAsync($"daily_log_create", $"Entry creata: {entry.Date:dd/MM/yyyy} - {entry.Client} - {entry.Hours}h");
        return entry;
    }

    public async Task<List<DailyLogEntry>> GetEntriesByDateAsync(DateTime date)
    {
        var rows = await _sheets.ReadRowsAsync("DailyLogs");
        var entries = new List<DailyLogEntry>();

        foreach (var row in rows.Skip(1)) // skip header
        {
            if (row.Count >= 2 && DateTime.TryParse(row[1] as string, out var entryDate) && entryDate.Date == date.Date)
            {
                entries.Add(new DailyLogEntry
                {
                    Id = row[0]?.ToString() ?? "",
                    Date = entryDate,
                    Client = row[2]?.ToString() ?? "",
                    Project = row.Count > 3 ? (row[3]?.ToString() ?? "") : "",
                    Hours = decimal.TryParse(row[4]?.ToString(), out var h) ? h : 0m,
                    Description = row.Count > 5 ? (row[5]?.ToString() ?? "") : ""
                });
            }
        }

        return entries;
    }

    public async Task<List<DailyLogEntry>> GetEntriesByMonthAsync(int year, int month)
    {
        var rows = await _sheets.ReadRowsAsync("DailyLogs");
        var entries = new List<DailyLogEntry>();

        foreach (var row in rows.Skip(1))
        {
            if (row.Count >= 2 && DateTime.TryParse(row[1] as string, out var entryDate) &&
                entryDate.Year == year && entryDate.Month == month)
            {
                entries.Add(new DailyLogEntry
                {
                    Id = row[0]?.ToString() ?? "",
                    Date = entryDate,
                    Client = row[2]?.ToString() ?? "",
                    Project = row.Count > 3 ? (row[3]?.ToString() ?? "") : "",
                    Hours = decimal.TryParse(row[4]?.ToString(), out var h) ? h : 0m,
                    Description = row.Count > 5 ? (row[5]?.ToString() ?? "") : ""
                });
            }
        }

        return entries;
    }

    public async Task<ClientInfo?> GetOrCreateClientAsync(string clientName)
    {
        var rows = await _sheets.ReadRowsAsync("Clients");

        foreach (var row in rows.Skip(1))
        {
            if (row.Count >= 2 && row[1]?.ToString()?.Equals(clientName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return new ClientInfo
                {
                    Id = row[0]?.ToString() ?? "",
                    Name = row[1]?.ToString() ?? clientName,
                    VatNumber = row.Count > 2 ? (row[2]?.ToString() ?? "") : null,
                    Active = true
                };
            }
        }

        // Crea nuovo cliente a conferma dell'utente
        return new ClientInfo
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Name = clientName,
            Active = true
        };
    }
}

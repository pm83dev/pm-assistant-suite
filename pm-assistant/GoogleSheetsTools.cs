using Google.Apis.Sheets.v4;
using LocalCodeAgent.Models;
using PmAssistant.Services;

namespace LocalCodeAgent.Tools;

/// <summary>
/// Tool per interagire con Google Sheets (DailyLogs, Todos, AuditLog, Clients, EmailQueue).
/// </summary>
public class GoogleSheetsTools(IGoogleSheetsService sheetsService, GoogleSheetsSettings settings)
{
    private string _cachedSheetList = "";

    public List<ToolDefinition> Definitions =>
    [
        new() { Function = new() {
            Name = "sheets_read",
            Description = "Legge tutte le righe di un foglio Google Sheets. Usare sheets_list_sheets per vedere i fogli disponibili.",
            Parameters = new { type = "object",
                properties = new {
                    sheet_name = new { type = "string", description = "Nome del foglio (usare sheets_list_sheets per elencare)" }
                },
                required = new[] { "sheet_name" } }
        }},
        new() { Function = new() {
            Name = "sheets_append",
            Description = "Aggiunge una riga a un foglio Google Sheets. I valori vengono aggiunti come nuova riga alla fine.",
            Parameters = new { type = "object",
                properties = new {
                    sheet_name = new { type = "string", description = "Nome del foglio (usare sheets_list_sheets per elencare)" },
                    values     = new { type = "string",  description = "Valori separati da pipe '|' (es. '2026-07-19|ClienteA|ProgettoB|8.0|Descrizione task')" }
                },
                required = new[] { "sheet_name", "values" } }
        }},
        new() { Function = new() {
            Name = "sheets_create_sheet",
            Description = "Crea un nuovo foglio all'interno del Google Sheet esistente (se non esiste già).",
            Parameters = new { type = "object",
                properties = new {
                    sheet_name = new { type = "string", description = "Nome del nuovo foglio da creare" }
                },
                required = new[] { "sheet_name" } }
        }},
        new() { Function = new() {
            Name = "sheets_list_sheets",
            Description = "Elenca tutti i fogli disponibili nel Google Sheet.",
            Parameters = new { type = "object", properties = new { }, required = Array.Empty<string>() }
        }}
    ];

    public string Execute(string toolName, string argumentsJson)
    {
        try
        {
            var args = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(argumentsJson);
            return toolName switch
            {
                "sheets_read" => ReadSheet(args).GetAwaiter().GetResult(),
                "sheets_append" => AppendRow(args).GetAwaiter().GetResult(),
                "sheets_create_sheet" => CreateSheet(args).GetAwaiter().GetResult(),
                "sheets_list_sheets" => ListSheets().GetAwaiter().GetResult(),
                _ => $"Tool '{toolName}' non trovato."
            };
        }
        catch (Exception ex) { return $"ERRORE: {ex.Message}"; }
    }

    private async Task<string> ReadSheet(System.Text.Json.JsonElement args)
    {
        var sheetName = args.GetProperty("sheet_name").GetString()!;
        var rows = await sheetsService.ReadRowsAsync(sheetName);

        if (rows.Count == 0) return $"Foglio '{sheetName}' vuoto o non trovato.";

        var sb = new System.Text.StringBuilder();
        // Header
        sb.AppendLine(string.Join(" | ", rows[0]));
        
        // Data rows (limit to first 50 for context safety)
        for (int i = 1; i < Math.Min(rows.Count, 51); i++)
        {
            sb.AppendLine(string.Join(" | ", rows[i]));
        }

        if (rows.Count > 50)
            sb.AppendLine($"\n... (+{rows.Count - 50} altre righe - usa sheets_append per aggiungere nuove voci)");

        return sb.ToString();
    }

    private async Task<string> AppendRow(System.Text.Json.JsonElement args)
    {
        var sheetName = args.GetProperty("sheet_name").GetString()!;
        var valuesStr = args.GetProperty("values").GetString()!;
        var values = valuesStr.Split('|').Select(v => (object)v.Trim()).ToArray();

        await sheetsService.AppendRowsAsync(sheetName, values);
        return $"✓ Riga aggiunta a '{sheetName}': {string.Join(", ", values)}";
    }

    private async Task<string> CreateSheet(System.Text.Json.JsonElement args)
    {
        var sheetName = args.GetProperty("sheet_name").GetString()!;
        await sheetsService.GetOrCreateSheetAsync(sheetName);
        return $"✓ Foglio '{sheetName}' creato o già esistente.";
    }

    private async Task<string> ListSheets()
    {
        var sheetNames = await sheetsService.ListSheetsAsync();
        _cachedSheetList = string.Join(", ", sheetNames);
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Fogli nel Google Sheet '{settings.SheetId}':");
        foreach (var name in sheetNames)
        {
            sb.AppendLine($"  - {name}");
        }
        sb.AppendLine($"\nTotale: {sheetNames.Count} fogli");
        return sb.ToString();
    }

    /// <summary>
    /// Gets the cached list of sheet names for use in tool descriptions.
    /// Call RefreshSheetList() first to populate.
    /// </summary>
    public string GetDynamicSheetDescriptions()
    {
        if (string.IsNullOrEmpty(_cachedSheetList))
        {
            return "Fogli non ancora caricati - usare sheets_list_sheets prima.";
        }
        return _cachedSheetList;
    }

    /// <summary>
    /// Refreshes the cached sheet list by calling the API.
    /// Call this after creating new sheets to ensure the agent sees them.
    /// </summary>
    public async Task RefreshSheetList()
    {
        var sheetNames = await sheetsService.ListSheetsAsync();
        _cachedSheetList = string.Join(", ", sheetNames);
    }
}

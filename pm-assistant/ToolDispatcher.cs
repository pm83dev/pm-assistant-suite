using System.Text.Json;
using System.Text.RegularExpressions;
using LocalCodeAgent.Core;
using LocalCodeAgent.Models;
using LocalCodeAgent.Tools;
using Microsoft.Extensions.Configuration;
using PmAssistant.Services;
using PmAssistant.Tools;
using PmAssistant.Models.Dtos.Chat;

/// <summary>
/// Registro e dispatcher centralizzato per tutti i tool dell'agente.
/// Include soft-block su read_file duplicati e invalidazione cache su write/edit.
/// </summary>
public class ToolDispatcher
{
    private readonly FileSystemTools _fs;
    private readonly TerminalTools _terminal;
    private readonly AgentTools _agent;
    private readonly GitTools _git;
    private readonly WebTools _web;
    private readonly GoogleSheetsTools _sheets;
    private readonly WordDocTools _worddoc;
    private readonly OreTrackingTools _ore;

    // Cache read_file: path → contenuto già letto (soft-block su duplicati)
    private readonly Dictionary<string, string> _fileCache =
        new(StringComparer.OrdinalIgnoreCase);


    public ToolDispatcher(WorkspaceContext workspace, string? googleSearchApiKey = null, string? googleSearchCx = null, IGoogleSheetsService? sheetsService = null, string? oreTrackingBaseUrl = null)
    {
        _fs = new FileSystemTools(workspace);
        _terminal = new TerminalTools(workspace);
        _agent = new AgentTools(workspace);
        _git = new GitTools(workspace);
        _web = new WebTools(googleSearchApiKey, googleSearchCx);
        _ore = new OreTrackingTools(string.IsNullOrWhiteSpace(oreTrackingBaseUrl) ? "http://localhost:5108" : oreTrackingBaseUrl);

        // Initialize GoogleSheetsTools if service is provided (optional for offline mode)
        _sheets = sheetsService != null
            ? CreateGoogleSheetsTools(sheetsService)
            : CreateOfflineGoogleSheetsTools();

        _worddoc = new WordDocTools(workspace);
    }

    private static GoogleSheetsTools CreateGoogleSheetsTools(IGoogleSheetsService sheetsService)
    {
        // Try to load settings from appsettings.json
        try
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            var section = config.GetSection("GoogleSheets");
            if (!string.IsNullOrEmpty(section["SheetId"]))
            {
                var settings = new GoogleSheetsSettings
                {
                    SheetId = section["SheetId"] ?? "",
                    DailyLogsSheetName = section["DailyLogsSheetName"] ?? "DailyLogs",
                    TodosSheetName = section["TodosSheetName"] ?? "Todos",
                    AuditLogSheetName = section["AuditLogSheetName"] ?? "AuditLog",
                    ClientsSheetName = section["ClientsSheetName"] ?? "Clients",
                    EmailQueueSheetName = section["EmailQueueSheetName"] ?? "EmailQueue",
                    ServiceAccountJsonPath = section["ServiceAccountJsonPath"] ?? "./keys/service-account.json"
                };
                return new GoogleSheetsTools(sheetsService, settings);
            }
        }
        catch { /* Fall through to offline mode */ }

        // Fallback: offline mode with defaults
        var defaultSettings = new GoogleSheetsSettings();
        return new GoogleSheetsTools(null!, defaultSettings);
    }

    private static GoogleSheetsTools CreateOfflineGoogleSheetsTools()
    {
        var defaultSettings = new GoogleSheetsSettings();
        return new GoogleSheetsTools(null!, defaultSettings);
    }

    public List<ToolDefinition> AllDefinitions =>
    [
        .._fs.Definitions,
        .._terminal.Definitions,
        .._git.Definitions,
        .._agent.Definitions,
        .._web.Definitions,
        .._sheets.Definitions,
        .._worddoc.Definitions,
        .._ore.Definitions
    ];

    public List<ToolDefinition> GetDefinitionsByCategory(string category) =>
        category.ToLowerInvariant() switch
        {
            "filesystem" => [.. _fs.Definitions],
            "terminal" => [.. _terminal.Definitions],
            "git" => [.. _git.Definitions],
            "agent" => [.. _agent.Definitions],
            "web" => [.. _web.Definitions],
            "sheets" => [.. _sheets.Definitions],
            "ore" => [.. _ore.Definitions],
            _ => []
        };

    public Dictionary<string, ToolInfo> GetToolDefinitions() =>
        AllDefinitions.ToDictionary(
            t => t.Function.Name,
            t => new ToolInfo
            {
                Name = t.Function.Name,
                Description = t.Function.Description,
                Parameters = JsonSerializer.Serialize(t.Function.Parameters),
                IsAvailable = true
            });

    public List<ToolDefinition> GetDefinitionsByName(string name) =>
        AllDefinitions.Where(t => t.Function.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();


    // Restituisce solo i tool rilevanti per il contesto del messaggio utente.
    // Riduce il prompt del 60-80% rispetto ad AllDefinitions.
    public List<ToolDefinition> GetRelevantDefinitions(string context)
    {
        var lc = context.ToLowerInvariant();
        var defs = new List<ToolDefinition>();

        // Core — sempre inclusi (operazioni file + ricerca + workspace + build + shell)
        defs.AddRange(_fs.Definitions.Where(t => t.Function.Name is
            "read_file" or "edit_file" or "write_file" or
            "search_in_files" or "sym_search" or "glob_files"));
        defs.Add(_agent.Definitions.First(t => t.Function.Name == "get_workspace_tree"));
        defs.Add(_terminal.Definitions.First(t => t.Function.Name == "sol_analyze"));
        defs.AddRange(_terminal.Definitions.Where(t => t.Function.Name is
            "run_command" or "get_background_output" or "stop_background_job" or "list_background_jobs"));
        // Core — sempre inclusi (operazioni file + ricerca + workspace + build + shell)
        defs.AddRange(_fs.Definitions.Where(t => t.Function.Name is
            "read_file" or "edit_file" or "write_file" or
            "search_in_files" or "sym_search" or "glob_files" or
            "read_file_range"));

        // Terminal — su task che richiedono dotnet specifico
        if (Has(lc, "test", "package", "nuget", "add ", "pubbl", "run_dotnet"))
            defs.Add(_terminal.Definitions.First(t => t.Function.Name == "run_dotnet"));

        // Navigazione directory — su task che richiedono gestione cartelle/workspace
        if (Has(lc, "dir", "cartell", "folder", "list", "sposta", "rinomina", "elimina", "cancella", "struttura", "workspace", "init"))
        {
            defs.AddRange(_fs.Definitions.Where(t => t.Function.Name is
                "list_directory" or "create_directory" or "move_file" or "delete_file"));
            defs.AddRange(_agent.Definitions.Where(t => t.Function.Name is "dir_info" or "set_workspace"));
        }



        // Git — su task git
        if (Has(lc, "git", "commit", "push", "branch", "diff", "merge", "stash", "log", "stage"))
            defs.AddRange(_git.Definitions);

        // Web — su task che richiedono accesso internet o documentazione online
        if (Has(lc, "web", "http", "url", "cerca online", "internet", "documentazion", "fetch", "download", "sito", "api key", "nuget.org"))
            defs.AddRange(_web.Definitions);

        // Sheets — su task che richiedono Google Sheets
        if (Has(lc, "sheet", "sheets", "foglio", "google sheets", "dailylog", "todo", "auditlog", "client", "emailqueue"))
            defs.AddRange(_sheets.Definitions);

        // Word document — su task che richiedono creazione documenti
        if (Has(lc, "word", "docx", "documento word", "crea documento", "create_word_doc"))
            defs.AddRange(_worddoc.Definitions);

        // Ore tracking — su task che richiedono ore/clienti/progetti/note del time-tracking
        if (Has(lc, "ora", "ore", "cliente", "clienti", "progetto", "progetti", "fattur", "nota", "note lavorate", "time tracking", "time-tracking"))
            defs.AddRange(_ore.Definitions);

        return [.. defs.DistinctBy(t => t.Function.Name)];
    }

    private static bool Has(string s, params string[] kw) =>
        kw.Any(k => s.Contains(k, StringComparison.OrdinalIgnoreCase));

    public void ClearCache() => _fileCache.Clear();

    public void ResetCwd() => _terminal.ResetCwd();

    public string Execute(string toolName, string argumentsJson)
    {
        // ── Gestione read_file_range ──────────────────────────────────────
        if (toolName == "read_file_range")
        {
            try
            {
                var el = JsonDocument.Parse(argumentsJson).RootElement;
                var path = el.GetProperty("path").GetString() ?? "";
                var startLine = el.GetProperty("start_line").GetInt32();
                var endLine = el.GetProperty("end_line").GetInt32();

                var result = _fs.Execute(toolName, argumentsJson);
                return result;
            }
            catch { /* Passa al routing normale */ }
        }

        // ── Invalidazione cache su write/edit ─────────────────────────────────
        if (toolName is "write_file" or "edit_file" or "delete_file" or "move_file")
        {
            try
            {
                var el = JsonDocument.Parse(argumentsJson).RootElement;
                if (el.TryGetProperty("path", out var pathProp))
                    _fileCache.Remove(pathProp.GetString() ?? "");
                // move_file: invalida anche sorgente
                if (toolName == "move_file" && el.TryGetProperty("source", out var srcProp))
                    _fileCache.Remove(srcProp.GetString() ?? "");
            }
            catch { }
        }

        // ── Validazione JSON pre-dispatch ────────────────────────────────────
        // Intercetta JSON malformato (es. virgolette non escapate in content/old_string)
        // prima che raggiunga il singolo tool, dove l'errore sarebbe generico e inutile.
        if (toolName is "write_file" or "edit_file" or "move_file" or "delete_file" or "create_directory")
        {
            try { JsonDocument.Parse(argumentsJson); }
            catch (System.Text.Json.JsonException ex)
            {
                return $"ERRORE JSON argomenti (colonna ~{ex.BytePositionInLine}): virgolette o caratteri speciali non escapati " +
                       $"in 'content' o 'old_string'.\n" +
                       $"REGOLE anti-errore:\n" +
                       $"• edit_file: usa old_string con MAX 10 righe — non copiare interi metodi\n" +
                       $"• write_file: solo per file senza virgolette nel codice (config, plain text)\n" +
                       $"• Per codice C# con $\", [Attr(\"..\")], ecc.: crea prima la struttura " +
                       $"base con write_file (classe vuota), poi aggiungi il contenuto con edit_file riga per riga.";
            }
        }

        // ── Routing ───────────────────────────────────────────────────────────
        if (_fs.Definitions.Any(t => t.Function.Name == toolName))
            return _fs.Execute(toolName, argumentsJson);

        if (_terminal.Definitions.Any(t => t.Function.Name == toolName))
            return _terminal.Execute(toolName, argumentsJson);

        if (_git.Definitions.Any(t => t.Function.Name == toolName))
            return _git.Execute(toolName, argumentsJson);

        if (_agent.Definitions.Any(t => t.Function.Name == toolName))
        {
            var result = _agent.Execute(toolName, argumentsJson);
            if (toolName == "set_workspace") _terminal.ResetCwd();
            return result;
        }

        if (_web.Definitions.Any(t => t.Function.Name == toolName))
            return _web.Execute(toolName, argumentsJson);

        // Google Sheets — su task che richiedono Google Sheets
        if (toolName.StartsWith("sheets_"))
            return _sheets.Execute(toolName, argumentsJson);

        // Word document tools
        if (_worddoc.Definitions.Any(t => t.Function.Name == toolName))
            return _worddoc.Execute(toolName, argumentsJson);

        // Ore tracking (ore-tracking/Api via HTTP)
        if (toolName.StartsWith("ore_"))
            return _ore.Execute(toolName, argumentsJson);

        return $"Tool '{toolName}' non registrato nel dispatcher.";
    }

    // Metodi asincroni per tool che supportano operazioni I/O (es. OreTrackingTools)
    public async Task<string> ExecuteAsync(string toolName, string argumentsJson)
    {
        // Ore tracking (ore-tracking/Api via HTTP) - unico tool con metodi asincroni al momento
        if (toolName.StartsWith("ore_"))
            return await _ore.ExecuteAsync(toolName, argumentsJson);

        // Fallback: esegui sincrono
        return Execute(toolName, argumentsJson);
    }

    private static string BuildFileSummary(string path, string cachedContent)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var lines = cachedContent.Split('\n');
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"File già letto: '{path}' ({lines.Length} righe)");
        sb.AppendLine("Riepilogo:");

        if (ext == ".cs")
        {
            var types = Regex.Matches(cachedContent,
                @"(?:public|private|protected|internal|static|abstract|sealed)\s+(?:class|interface|enum|record|struct)\s+(\w+)",
                RegexOptions.Multiline)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct();

            var typesStr = string.Join(", ", types);
            if (!string.IsNullOrEmpty(typesStr))
                sb.AppendLine($"- Tipi: {typesStr}");

            var methods = Regex.Matches(cachedContent,
                @"(?:public|private|protected|internal|static|async|override|virtual)\s+[\w<>\[\]?]+\s+(\w+)\s*\(",
                RegexOptions.Multiline)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Where(n => n is not ("if" or "while" or "for" or "foreach" or "switch" or "return"))
                .Distinct()
                .Take(15);

            var methodsStr = string.Join(", ", methods);
            if (!string.IsNullOrEmpty(methodsStr))
                sb.AppendLine($"- Metodi: {methodsStr}");
        }
        else
        {
            var preview = lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Take(4)
                .Select(l => $"  {l.Trim()[..Math.Min(80, l.Trim().Length)]}");
            sb.AppendLine(string.Join('\n', preview));
        }

        sb.AppendLine("Usa edit_file per modificarlo, o read_file con start_line/end_line per una sezione.");
        return sb.ToString().TrimEnd();
    }
}

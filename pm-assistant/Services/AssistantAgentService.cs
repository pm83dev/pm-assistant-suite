using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalCodeAgent.Core;
using LocalCodeAgent.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace PmAssistant.Services;

/// <summary>
/// Agente conversazionale con tool calling (loop ReAct): l'LLM decide autonomamente
/// quali dati leggere dallo sheet e quali azioni proporre, invece di ricevere un
/// contesto precompilato. Le scritture sul registro ore passano sempre da una
/// conferma esplicita dell'utente gestita in modo deterministico dal C#.
/// </summary>
public interface IAssistantAgentService
{
    Task<string> ProcessAsync(string message, string sessionId);
}

public class AssistantAgentService : IAssistantAgentService
{
    private readonly LlamaClient _llm;
    private readonly ToolDispatcher _toolDispatcher;
    private readonly IGoogleSheetsService _sheets;
    private readonly IGuideService _guides;
    private readonly ILogger<AssistantAgentService> _logger;

    private const int MaxSteps = 8;

    private sealed record StagedLog(DateTime Date, string Client, string Project, decimal Hours, string Description);
    private readonly ConcurrentDictionary<string, List<StagedLog>> _pending = new();

    private static readonly JsonSerializerOptions _jsonOut = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AssistantAgentService(IOptions<LlmSettings> llmSettings, ToolDispatcher toolDispatcher,
        IGoogleSheetsService sheets, IGuideService guides, ILogger<AssistantAgentService> logger)
    {
        _llm = new LlamaClient(llmSettings.Value.BaseUrl, llmSettings.Value.ModelName);
        _toolDispatcher = toolDispatcher;
        _sheets = sheets;
        _guides = guides;
        _logger = logger;
    }

    public async Task<string> ProcessAsync(string message, string sessionId)
    {
        var normalized = message.Trim().ToLowerInvariant();

        // Conferma/annullamento di un inserimento in sospeso: deterministico, niente LLM
        if (_pending.TryRemove(sessionId, out var pending))
        {
            if (normalized is "sì" or "si" or "ok" or "conferma" or "yes")
                return await WriteStagedAsync(sessionId, pending);

            if (normalized is "no" or "annulla" or "cancella")
                return "Inserimento annullato, non ho registrato nulla.";

            _pending[sessionId] = pending;
            return "Hai un inserimento in sospeso: rispondi 'sì' per confermare o 'no' per annullare.";
        }

        var messages = new List<ChatMessage>
        {
            ChatMessage.System(BuildSystemPrompt()),
            ChatMessage.User(message)
        };

        var correctionSent = false;
        for (var step = 0; step < MaxSteps; step++)
        {
            var response = await _llm.ChatAsync(new ChatRequest
            {
                Messages = messages,
                Tools = BuildTools(),
                MaxTokens = 2048
            });

            var reply = response?.Choices.FirstOrDefault()?.Message;
            if (reply is null)
                return "Non ho ricevuto risposta dal modello. Riprova tra qualche istante.";

            messages.Add(reply);

            if (reply.ToolCalls is { Count: > 0 })
            {
                foreach (var call in reply.ToolCalls)
                {
                    var result = await ExecuteToolAsync(call, sessionId);
                    _logger.LogInformation("Tool {Name}({Args}) -> {Result}",
                        call.Function.Name, Truncate(call.Function.Arguments, 200), Truncate(result, 200));
                    messages.Add(ChatMessage.ToolResult(call.Id, result));
                }
                continue;
            }

            if (!string.IsNullOrWhiteSpace(reply.Content))
            {
                var text = reply.Content.Trim();

                // Guardrail: se chiede conferma ma non ha fatto lo staging, un "sì" dell'utente
                // cadrebbe nel vuoto → lo si costringe a chiamare davvero il tool
                if (!correctionSent && !_pending.ContainsKey(sessionId) &&
                    text.Contains("conferma", StringComparison.OrdinalIgnoreCase))
                {
                    correctionSent = true;
                    _logger.LogWarning("Il modello ha chiesto conferma senza staging: correzione forzata");
                    messages.Add(ChatMessage.User(
                        "ATTENZIONE: non hai chiamato stage_daily_logs, quindi NON c'è nulla in sospeso " +
                        "da confermare. Chiama ORA il tool stage_daily_logs con le voci da registrare."));
                    continue;
                }

                return text;
            }

            return "Non ho una risposta per questo messaggio. Prova a riformulare.";
        }

        return "Non sono riuscito a completare la richiesta in un numero ragionevole di passaggi. Prova a riformulare.";
    }

    // ── System prompt ─────────────────────────────────────────────────────────

    private static string BuildSystemPrompt()
    {
        var it = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
        var recentDays = string.Join("; ", Enumerable.Range(0, 8).Select(i =>
        {
            var d = DateTime.Today.AddDays(-i);
            var label = i switch { 0 => " (OGGI)", 1 => " (ieri)", _ => "" };
            return $"{it.DateTimeFormat.GetDayName(d.DayOfWeek)} {d:yyyy-MM-dd}{label}";
        }));

        return
        "Sei l'assistente personale di un freelance: gestisci registro ore, task e guide.\n" +
        $"Calendario recente: {recentDays}.\n" +
        "Quando l'utente cita un giorno della settimana senza data ('venerdì', 'ieri', 'lunedì scorso') " +
        "usa il calendario recente qui sopra: scegli la data corrispondente più recente nel passato, non chiedere la data.\n" +
        "REGOLA FONDAMENTALE: i dati esistono SOLO nei tool. Prima di rispondere a qualunque domanda " +
        "su ore/attività chiama get_daily_logs, sui task chiama get_todos, sulle guide chiama " +
        "list_guides o read_guide. Non rispondere MAI a memoria e non inventare mai valori: " +
        "se un tool restituisce elenchi vuoti, dì che non ci sono dati.\n" +
        "Per i totali usa i campi 'totale_*' restituiti dai tool, non ricalcolarli.\n" +
        "Per registrare attività lavorative DEVI chiamare il tool stage_daily_logs: non descrivere " +
        "mai la registrazione solo a parole, senza tool non viene preparato nulla. " +
        "Chiama stage_daily_logs con le voci interpretate " +
        "(salta righe con 0 ore; anni a due cifre = 20xx; mesi italiani: gen=01 feb=02 mar=03 apr=04 " +
        "mag=05 giu=06 lug=07 ago=08 set=09 ott=10 nov=11 dic=12; se una riga ha due numeri, il primo " +
        "è un importo da ignorare e il secondo sono le ore). Dopo stage_daily_logs riassumi le voci " +
        "all'utente e chiedi conferma con 'sì'.\n" +
        "Rispondi in italiano, breve e concreto, in testo semplice senza markdown (**, ##, tabelle).";
    }

    // ── Tool definitions ──────────────────────────────────────────────────────
    // Restituisce TUTTI i tool disponibili + i tool custom dell'agente (daily_logs, todos)

    private List<ToolDefinition> BuildTools()
    {
        // Prendi tutte le definizioni da ToolDispatcher (filesystem, terminal, git, web, sheets, agent)
        var allDefs = _toolDispatcher.AllDefinitions.ToList();

        // Aggiungi i tool custom dell'agente che NON sono in ToolDispatcher
        var existingNames = new HashSet<string>(allDefs.Select(t => t.Function.Name), StringComparer.OrdinalIgnoreCase);
        
        foreach (var customTool in CustomTools())
        {
            if (!existingNames.Contains(customTool.Function.Name))
                allDefs.Add(customTool);
        }

        return allDefs;
    }

    private static List<ToolDefinition> CustomTools() =>
    [
        Tool("get_daily_logs",
            "Legge le attività registrate (registro ore) di un mese: voci con data, cliente, progetto, ore, descrizione, più totali già calcolati per progetto e cliente.",
            new
            {
                type = "object",
                properties = new
                {
                    year = new { type = "integer", description = "Anno, es. 2026" },
                    month = new { type = "integer", description = "Mese 1-12" }
                },
                required = new[] { "year", "month" }
            }),
        Tool("get_todos",
            "Legge l'elenco dei task (aperti e completati) con progetto e scadenza.",
            new { type = "object", properties = new { } }),
        Tool("stage_daily_logs",
            "Prepara la registrazione di una o più attività lavorative. NON scrive subito: l'utente dovrà confermare con 'sì'.",
            new
            {
                type = "object",
                properties = new
                {
                    entries = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                date = new { type = "string", description = "Data in formato YYYY-MM-DD" },
                                client = new { type = "string", description = "Nome cliente, vuoto se non noto" },
                                project = new { type = "string", description = "Nome progetto, vuoto se non noto" },
                                hours = new { type = "number", description = "Ore lavorate, maggiore di 0" },
                                description = new { type = "string" }
                            },
                            required = new[] { "date", "hours" }
                        }
                    }
                },
                required = new[] { "entries" }
            }),
        Tool("add_todo",
            "Aggiunge subito un task/promemoria alla lista.",
            new
            {
                type = "object",
                properties = new
                {
                    title = new { type = "string" },
                    project = new { type = "string", description = "Opzionale" },
                    due_date = new { type = "string", description = "Scadenza YYYY-MM-DD, opzionale" }
                },
                required = new[] { "title" }
            }),
        Tool("list_guides",
            "Elenca i nomi delle guide disponibili.",
            new { type = "object", properties = new { } }),
        Tool("read_guide",
            "Legge il contenuto di una guida.",
            new
            {
                type = "object",
                properties = new { name = new { type = "string", description = "Nome della guida senza estensione" } },
                required = new[] { "name" }
            })
    ];

    private static ToolDefinition Tool(string name, string description, object parameters) =>
        new() { Function = new FunctionDefinition { Name = name, Description = description, Parameters = parameters } };

    // ── Tool execution ────────────────────────────────────────────────────────

    private async Task<string> ExecuteToolAsync(ToolCall call, string sessionId)
    {
        try
        {
            // I tool custom dell'agente hanno la loro logica di esecuzione
            var customTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "get_daily_logs", "get_todos", "stage_daily_logs", 
                "add_todo", "list_guides", "read_guide"
            };

            if (customTools.Contains(call.Function.Name))
            {
                using var args = JsonDocument.Parse(
                    string.IsNullOrWhiteSpace(call.Function.Arguments) ? "{}" : call.Function.Arguments);
                var root = args.RootElement;

                return call.Function.Name switch
                {
                    "get_daily_logs" => await GetDailyLogsAsync(
                        root.GetProperty("year").GetInt32(), root.GetProperty("month").GetInt32()),
                    "get_todos" => await GetTodosAsync(),
                    "stage_daily_logs" => await StageDailyLogsAsync(root, sessionId),
                    "add_todo" => await AddTodoAsync(root),
                    "list_guides" => JsonSerializer.Serialize(new { guide = await _guides.ListGuidesAsync() }, _jsonOut),
                    "read_guide" => JsonSerializer.Serialize(new
                    {
                        contenuto = Truncate(await _guides.GetGuideAsync(root.GetProperty("name").GetString() ?? ""), 3000)
                    }, _jsonOut),
                    _ => throw new InvalidOperationException($"Tool custom sconosciuto: {call.Function.Name}")
                };
            }

            // Per tutti gli altri tool (filesystem, terminal, git, web, sheets, agent)
            // usa il ToolDispatcher che gestisce tutto in modo centralizzato
            using var args2 = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(call.Function.Arguments) ? "{}" : call.Function.Arguments);
            var argsJson = args2.RootElement.GetRawText();

            var result = _toolDispatcher.Execute(call.Function.Name, argsJson);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore esecuzione tool {Name}", call.Function.Name);
            return JsonSerializer.Serialize(new { errore = ex.Message }, _jsonOut);
        }
    }

    private async Task<string> GetDailyLogsAsync(int year, int month)
    {
        var rows = await _sheets.ReadRowsAsync("DailyLogs");
        var entries = new List<object>();
        decimal total = 0;
        var byProject = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var byClient = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.Skip(1))
        {
            if (row.Count < 5 || !DateTime.TryParse(row[1]?.ToString(), out var date))
                continue;
            if (date.Year != year || date.Month != month)
                continue;

            decimal.TryParse(row[4]?.ToString(), out var hours);
            var client = row[2]?.ToString() ?? "";
            var project = row.Count > 3 ? row[3]?.ToString() ?? "" : "";

            total += hours;
            if (!string.IsNullOrWhiteSpace(project))
                byProject[project] = byProject.GetValueOrDefault(project) + hours;
            if (!string.IsNullOrWhiteSpace(client))
                byClient[client] = byClient.GetValueOrDefault(client) + hours;

            entries.Add(new
            {
                data = date.ToString("yyyy-MM-dd"),
                cliente = client,
                progetto = project,
                ore = hours,
                descrizione = row.Count > 5 ? row[5]?.ToString() ?? "" : ""
            });
        }

        return JsonSerializer.Serialize(new
        {
            voci = entries,
            totale_ore = total,
            totale_per_progetto = byProject,
            totale_per_cliente = byClient
        }, _jsonOut);
    }

    private async Task<string> GetTodosAsync()
    {
        var rows = await _sheets.ReadRowsAsync("Todos");
        var open = new List<object>();
        var done = new List<object>();

        foreach (var row in rows.Skip(1))
        {
            if (row.Count < 3 || string.IsNullOrWhiteSpace(row[1]?.ToString()))
                continue;

            var todo = new
            {
                titolo = row[1]?.ToString() ?? "",
                progetto = row.Count > 3 ? row[3]?.ToString() ?? "" : "",
                scadenza = row.Count > 4 ? row[4]?.ToString() ?? "" : ""
            };

            if (string.Equals(row[2]?.ToString(), "TRUE", StringComparison.OrdinalIgnoreCase))
                done.Add(todo);
            else
                open.Add(todo);
        }

        return JsonSerializer.Serialize(new { aperti = open, completati = done }, _jsonOut);
    }

    private Task<string> StageDailyLogsAsync(JsonElement root, string sessionId)
    {
        var staged = new List<StagedLog>();

        if (root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in entries.EnumerateArray())
            {
                if (!e.TryGetProperty("date", out var d) || !DateTime.TryParse(d.GetString(), out var date))
                    continue;

                decimal hours = 0;
                if (e.TryGetProperty("hours", out var h))
                    hours = h.ValueKind == JsonValueKind.Number
                        ? h.GetDecimal()
                        : decimal.TryParse(h.GetString(), out var hp) ? hp : 0;
                if (hours <= 0)
                    continue;

                staged.Add(new StagedLog(
                    date,
                    e.TryGetProperty("client", out var c) ? c.GetString() ?? "" : "",
                    e.TryGetProperty("project", out var p) ? p.GetString() ?? "" : "",
                    hours,
                    e.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : ""));
            }
        }

        if (staged.Count == 0)
            return Task.FromResult(JsonSerializer.Serialize(
                new { errore = "Nessuna voce valida: servono almeno data e ore > 0." }, _jsonOut));

        _pending[sessionId] = staged;
        return Task.FromResult(JsonSerializer.Serialize(new
        {
            voci_in_sospeso = staged.Count,
            totale_ore = staged.Sum(s => s.Hours),
            istruzione = "Riassumi le voci all'utente e chiedigli di rispondere 'sì' per registrarle o 'no' per annullare."
        }, _jsonOut));
    }

    private async Task<string> AddTodoAsync(JsonElement root)
    {
        var title = root.GetProperty("title").GetString() ?? "";
        if (string.IsNullOrWhiteSpace(title))
            return JsonSerializer.Serialize(new { errore = "Titolo mancante." }, _jsonOut);

        var id = Guid.NewGuid().ToString("N")[..8];
        await _sheets.AppendRowsAsync("Todos",
            id, title.Trim(), false,
            root.TryGetProperty("project", out var p) ? p.GetString() ?? "" : "",
            root.TryGetProperty("due_date", out var d) ? d.GetString() ?? "" : "",
            DateTime.UtcNow.ToString("o"));

        return JsonSerializer.Serialize(new { aggiunto = title.Trim() }, _jsonOut);
    }

    private async Task<string> WriteStagedAsync(string sessionId, List<StagedLog> pending)
    {
        var written = 0;
        try
        {
            foreach (var e in pending)
            {
                var id = Guid.NewGuid().ToString("N")[..8];
                await _sheets.AppendRowsAsync("DailyLogs",
                    id, e.Date.ToString("yyyy-MM-dd"), e.Client, e.Project, e.Hours,
                    e.Description, DateTime.UtcNow.ToString("o"));
                written++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore scrittura attività confermate ({Written}/{Total})", written, pending.Count);
            _pending[sessionId] = pending.Skip(written).ToList();
            return $"Errore durante la scrittura ({written}/{pending.Count} registrate): {ex.Message}\n" +
                "Le rimanenti sono ancora in sospeso: rispondi 'sì' per riprovare o 'no' per annullare.";
        }

        return $"Registrate {pending.Count} attività per un totale di {pending.Sum(e => e.Hours)}h. Usa /logs per l'elenco.";
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";
}

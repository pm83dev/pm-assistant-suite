using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PmAssistant.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PmAssistant.Services;

public interface ITelegramBotService
{
    Task StartPollingAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(string chatId, string text, CancellationToken ct = default);
    Task<string> ProcessMessageAsync(string message, string sessionId = "api");
}

public class TelegramSettings
{
    public string BotToken { get; set; } = "";
    public string WebhookUrl { get; set; } = "";
    public int PollingTimeout { get; set; } = 60;
}

public class TelegramBotService : ITelegramBotService
{
    private readonly TelegramBotClient _botClient;
    private readonly ILlmService _llmService;
    private readonly IGoogleSheetsService _sheetsService;
    private readonly IAssistantAgentService _agent;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(IOptions<TelegramSettings> settings, ILlmService llmService,
        IGoogleSheetsService sheetsService, IAssistantAgentService agent, ILogger<TelegramBotService> logger)
    {
        // Se il token è vuoto o "bot-token" (valore fittizio), non inizializzare il client
        if (string.IsNullOrWhiteSpace(settings.Value.BotToken) ||

            settings.Value.BotToken.Equals("bot-token", StringComparison.OrdinalIgnoreCase))
        {
            _botClient = null!;
            _logger.LogWarning("Telegram bot token non configurato o fittizio. Il servizio Telegram sarà disabilitato.");
        }
        else
        {
            _botClient = new TelegramBotClient(settings.Value.BotToken);
        }
        _llmService = llmService;
        _sheetsService = sheetsService;
        _agent = agent;
        _logger = logger;
    }

    public async Task StartPollingAsync(CancellationToken cancellationToken = default)
    {
        if (_botClient == null)
        {
            _logger.LogInformation("Telegram bot non inizializzato, polling disabilitato");
            return;
        }

        try
        {
            await _botClient.ReceiveAsync(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Message is { } message && !string.IsNullOrWhiteSpace(message.Text))
            {
                var chatId = message.Chat.Id.ToString();
                var text = message.Text.Trim();

                await HandleCommandAsync(chatId, text, botClient, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore elaborazione messaggio Telegram");
            if (update.Message?.Chat.Id is { } errorChatId)
            {
                try
                {
                    await botClient.SendMessage(errorChatId.ToString(),
                        $"Si è verificato un errore: {ex.Message}", cancellationToken: ct);
                }
                catch { /* canale non disponibile, già loggato */ }
            }
        }
    }

    private async Task HandleCommandAsync(string chatId, string text, ITelegramBotClient botClient, CancellationToken ct)
    {
        if (text.StartsWith("/"))
        {
            await HandleSpecialCommandAsync(chatId, text, botClient, ct);
            return;
        }

        var response = await ProcessMessageAsync(text, chatId);
        await SendReplyAsync(botClient, chatId, response, ct);
    }

    /// <summary>
    /// Invio robusto: fallback a testo semplice se il Markdown non è valido per Telegram,
    /// troncamento sotto il limite di 4096 caratteri, gestione risposta vuota.
    /// </summary>
    private async Task SendReplyAsync(ITelegramBotClient botClient, string chatId, string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            await botClient.SendMessage(chatId,
                "Non ho ricevuto risposta dal modello. Riprova tra qualche istante.", cancellationToken: ct);
            return;
        }

        if (text.Length > 4000)
            text = text[..4000] + "\n\n... (risposta troncata)";

        try
        {
            await botClient.SendMessage(chatId, text, ParseMode.Markdown, cancellationToken: ct);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
        {
            _logger.LogWarning("Markdown rifiutato da Telegram ({Message}), reinvio come testo semplice", ex.Message);
            await botClient.SendMessage(chatId, text, cancellationToken: ct);
        }
    }

    private async Task HandleSpecialCommandAsync(string chatId, string text, ITelegramBotClient botClient, CancellationToken ct)
    {
        if (_botClient == null)
        {
            _logger.LogInformation("Telegram bot non inizializzato, comando disabilitato: {Command}", text);
            return;
        }

        var lower = text.ToLowerInvariant();

        if (lower == "/start")
        {
            await _botClient.SendMessage(chatId,
                "Benvenuto! Secretary AI Assistant pronto.\n\n" +
                "Comandi disponibili:\n" +
                "/log <data> <cliente> <ore> - Registra ore giornaliere\n" +
                "/logs [anno/mese] - Elenca le attività registrate\n" +
                "/todo <task> - Aggiungi un task\n" +
                "/todos - Lista tasks\n" +
                "/reconcile <anno>/<mese> - Riconciliazione mensile\n" +
                "/summary <anno>/<mese> - Riepilogo per Fiscozen\n" +
                "/guide [nome] - Consulta una guida\n" +
                "/update-guide <nome> <modifica> - Aggiorna guida (via staging)\n" +
                "/publish <nome> - Pubblica guida da staging a main",
                ParseMode.Markdown, cancellationToken: ct);
        }
        else if (lower == "/logs" || lower.StartsWith("/logs "))
        {
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month;
            var arg = text.Length > 5 ? text[5..].Trim() : "";
            if (!string.IsNullOrEmpty(arg))
            {
                var p = arg.Split('/');
                if (p.Length == 2 && int.TryParse(p[0], out var y) && int.TryParse(p[1], out var m))
                {
                    year = y;
                    month = m;
                }
                else
                {
                    await botClient.SendMessage(chatId,
                        "Formato non valido. Usa /logs oppure /logs <anno>/<mese>, es. /logs 2026/07",
                        cancellationToken: ct);
                    return;
                }
            }
            await HandleLogsListAsync(chatId, year, month, botClient, ct);
        }
        else if (lower.StartsWith("/log "))
        {
            var parts = text[5..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                if (DateTime.TryParse(parts[0], out var date) && decimal.TryParse(parts[2].Replace("h", ""), out var hours))
                {
                    await HandleDailyEntryAsync(chatId, date, parts[1], hours, parts.Length > 3 ? string.Join(' ', parts[3..]) : "", botClient, ct);
                }
            }
        }
        else if (lower.StartsWith("/todo "))
        {
            var title = text[6..].Trim();
            await HandleTodoAddAsync(chatId, title, botClient, ct);
        }
        else if (lower == "/todos")
        {
            await HandleTodosListAsync(chatId, botClient, ct);
        }
        else if (lower.StartsWith("/reconcile "))
        {
            var parts = text[11..].Split('/');
            if (parts.Length == 2 && int.TryParse(parts[0], out var year) && int.TryParse(parts[1], out var month))
            {
                await HandleReconcileAsync(chatId, year, month, botClient, ct);
            }
        }
        else if (lower.StartsWith("/summary "))
        {
            var parts = text[9..].Split('/');
            if (parts.Length == 2 && int.TryParse(parts[0], out var year) && int.TryParse(parts[1], out var month))
            {
                await HandleSummaryAsync(chatId, year, month, botClient, ct);
            }
        }
        else if (lower.StartsWith("/guide "))
        {
            var guideName = text[7..].Trim();
            await HandleGuideQueryAsync(chatId, guideName, botClient, ct);
        }
        else if (lower.StartsWith("/update-guide "))
        {
            var parts = text[14..].Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                await HandleGuideUpdateAsync(chatId, parts[0], parts[1], botClient, ct);
            }
        }
        else if (lower.StartsWith("/publish "))
        {
            var guideName = text[9..].Trim();
            await HandlePublishAsync(chatId, guideName, botClient, ct);
        }
    }

    public async Task<string> ProcessMessageAsync(string message, string sessionId = "api")
    {
        try
        {
            return await _agent.ProcessAsync(message, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agente non disponibile, fallback su risposta con contesto precompilato");
            return await AnswerQuestionAsync(message);
        }
    }

    private async Task<string> AnswerQuestionAsync(string message)
    {
        var context = await BuildDataContextAsync();

        var systemPrompt =
            "Sei l'assistente personale di un freelance. Rispondi in italiano, in modo breve e concreto.\n" +
            "Rispondi in testo semplice: niente markdown (**, ##, tabelle), usa trattini per gli elenchi.\n" +
            "Per domande su task e ore usa ESCLUSIVAMENTE i dati riportati sotto: se un dato non c'è, " +
            "dillo chiaramente senza inventare nulla.\n" +
            "Per i totali di ore usa i valori GIÀ CALCOLATI riportati nelle sezioni 'TOTALE ORE': " +
            "non sommare tu le singole voci.\n" +
            "Se l'utente vuole aggiungere task o registrare ore, ricordagli i comandi: " +
            "/todo <titolo>, /log <data> <cliente> <ore>, /logs per l'elenco attività, /todos per i task.\n\n" +
            context;

        return await _llmService.GenerateAsync($"L'utente ha scritto: {message}", systemPrompt);
    }

    private async Task<string> BuildDataContextAsync()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Data di oggi: {DateTime.Today:yyyy-MM-dd}");

        try
        {
            var todoRows = await _sheetsService.ReadRowsAsync("Todos");
            var open = new List<string>();
            var done = new List<string>();
            foreach (var row in todoRows.Skip(1))
            {
                if (row.Count < 3 || string.IsNullOrWhiteSpace(row[1]?.ToString()))
                    continue;

                var title = row[1]?.ToString();
                var project = row.Count > 3 ? row[3]?.ToString() : "";
                var due = row.Count > 4 ? row[4]?.ToString() : "";
                var line = $"- {title}"
                    + (string.IsNullOrEmpty(project) ? "" : $" [progetto: {project}]")
                    + (string.IsNullOrEmpty(due) ? "" : $" [scadenza: {due}]");

                if (IsTrue(row[2]))
                    done.Add(line);
                else
                    open.Add(line);
            }

            sb.AppendLine($"\nTASK APERTI ({open.Count}):");
            sb.AppendLine(open.Count > 0 ? string.Join('\n', open) : "(nessuno)");
            if (done.Count > 0)
            {
                sb.AppendLine($"\nTASK COMPLETATI ({done.Count}):");
                sb.AppendLine(string.Join('\n', done));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore lettura Todos per contesto LLM");
            sb.AppendLine("\n(elenco task non disponibile per un errore di lettura)");
        }

        try
        {
            var logRows = await _sheetsService.ReadRowsAsync("DailyLogs");
            var monthLines = new List<string>();
            decimal totalHours = 0;
            var byProject = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var byClient = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in logRows.Skip(1))
            {
                if (row.Count < 5 || !DateTime.TryParse(row[1]?.ToString(), out var date))
                    continue;
                if (date.Year != DateTime.Today.Year || date.Month != DateTime.Today.Month)
                    continue;

                decimal.TryParse(row[4]?.ToString(), out var hours);
                totalHours += hours;
                var client = row[2]?.ToString() ?? "";
                var project = row.Count > 3 ? row[3]?.ToString() ?? "" : "";
                var description = row.Count > 5 ? row[5]?.ToString() : "";

                if (!string.IsNullOrWhiteSpace(project))
                    byProject[project] = byProject.GetValueOrDefault(project) + hours;
                if (!string.IsNullOrWhiteSpace(client))
                    byClient[client] = byClient.GetValueOrDefault(client) + hours;

                monthLines.Add(
                    $"- {date:yyyy-MM-dd}: {hours}h | cliente: {client} | progetto: {project} | {description}".TrimEnd());
            }

            sb.AppendLine($"\nORE REGISTRATE QUESTO MESE (totale {totalHours}h):");
            sb.AppendLine(monthLines.Count > 0 ? string.Join('\n', monthLines) : "(nessuna)");

            if (byProject.Count > 0)
            {
                sb.AppendLine("\nTOTALE ORE PER PROGETTO questo mese (già calcolato, usa questi valori):");
                foreach (var kv in byProject.OrderByDescending(k => k.Value))
                    sb.AppendLine($"- {kv.Key}: {kv.Value}h");
            }

            if (byClient.Count > 0)
            {
                sb.AppendLine("\nTOTALE ORE PER CLIENTE questo mese (già calcolato, usa questi valori):");
                foreach (var kv in byClient.OrderByDescending(k => k.Value))
                    sb.AppendLine($"- {kv.Key}: {kv.Value}h");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore lettura DailyLogs per contesto LLM");
            sb.AppendLine("\n(registro ore non disponibile per un errore di lettura)");
        }

        return sb.ToString();
    }

    private static bool IsTrue(object? cell) =>
        string.Equals(cell?.ToString(), "TRUE", StringComparison.OrdinalIgnoreCase);

    private async Task HandleDailyEntryAsync(string chatId, DateTime date, string client, decimal hours,
        string description, ITelegramBotClient botClient, CancellationToken ct)
    {
        try
        {
            var id = Guid.NewGuid().ToString("N")[..8];
            await _sheetsService.AppendRowsAsync(
                "DailyLogs",
                id, date.ToString("yyyy-MM-dd"), client, "", hours, description, DateTime.UtcNow.ToString("o"));

            await botClient.SendMessage(chatId,
                $"Ore registrate: {date:dd/MM/yyyy} - {client} - {hours}h", ParseMode.Markdown, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore registrazione ore");
            await botClient.SendMessage(chatId, $"Errore nella registrazione: {ex.Message}", cancellationToken: ct);
        }
    }

    private async Task HandleLogsListAsync(string chatId, int year, int month,
        ITelegramBotClient botClient, CancellationToken ct)
    {
        try
        {
            var rows = await _sheetsService.ReadRowsAsync("DailyLogs");
            var lines = new List<string>();
            decimal totalHours = 0;
            foreach (var row in rows.Skip(1))
            {
                if (row.Count < 5 || !DateTime.TryParse(row[1]?.ToString(), out var date))
                    continue;
                if (date.Year != year || date.Month != month)
                    continue;

                decimal.TryParse(row[4]?.ToString(), out var hours);
                totalHours += hours;
                var description = row.Count > 5 ? row[5]?.ToString() : "";
                lines.Add($"{date:dd/MM} - {row[2]} - {hours}h {description}".TrimEnd());
            }

            if (lines.Count == 0)
            {
                await botClient.SendMessage(chatId,
                    $"Nessuna attività registrata per {month:D2}/{year}.", cancellationToken: ct);
                return;
            }

            var sb = new System.Text.StringBuilder($"Attività {month:D2}/{year} (totale {totalHours}h):\n\n");
            foreach (var line in lines)
                sb.AppendLine(line);

            await botClient.SendMessage(chatId, sb.ToString(), cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore lista attività");
            await botClient.SendMessage(chatId, $"Errore: {ex.Message}", cancellationToken: ct);
        }
    }

    private async Task HandleTodoAddAsync(string chatId, string title, ITelegramBotClient botClient, CancellationToken ct)
    {
        try
        {
            var id = Guid.NewGuid().ToString("N")[..8];
            await _sheetsService.AppendRowsAsync(
                "Todos",
                id, title, false, "", "", DateTime.UtcNow.ToString("o"));

            await botClient.SendMessage(chatId, $"Task aggiunto: {title}", ParseMode.Markdown, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore aggiunta todo");
            await botClient.SendMessage(chatId, $"Errore: {ex.Message}", cancellationToken: ct);
        }
    }

    private async Task HandleTodosListAsync(string chatId, ITelegramBotClient botClient, CancellationToken ct)
    {
        try
        {
            var rows = await _sheetsService.ReadRowsAsync("Todos");
            if (!rows.Any())
            {
                await botClient.SendMessage(chatId, "Nessun task presente.", cancellationToken: ct);
                return;
            }

            var sb = new System.Text.StringBuilder("Tasks:\n\n");
            foreach (var row in rows.Skip(1))
            {
                if (row.Count >= 2 && row[1] is string title)
                {
                    var completed = IsTrue(row[2]);
                    sb.AppendLine($"{(completed ? "[X]" : "[ ]")} {title}");
                }
            }

            await botClient.SendMessage(chatId, sb.ToString(), ParseMode.Markdown, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore lista todos");
            await botClient.SendMessage(chatId, $"Errore: {ex.Message}", cancellationToken: ct);
        }
    }

    private async Task HandleReconcileAsync(string chatId, int year, int month,
        ITelegramBotClient botClient, CancellationToken ct)
    {
        await botClient.SendMessage(chatId,
            $"Avvio riconciliazione {year}-{month:D2}...\nConfronto DailyLogs vs PDF cliente in corso.",
            ParseMode.Markdown, cancellationToken: ct);
    }

    private async Task HandleSummaryAsync(string chatId, int year, int month,
        ITelegramBotClient botClient, CancellationToken ct)
    {
        await botClient.SendMessage(chatId,
            $"Generazione riepilogo per {year}-{month:D2}...\nOutput testuale pronto per Fiscozen.",
            ParseMode.Markdown, cancellationToken: ct);
    }

    private async Task HandleGuideQueryAsync(string chatId, string guideName,
        ITelegramBotClient botClient, CancellationToken ct)
    {
        var filePath = Path.Combine("guides", $"{guideName}.md");
        if (File.Exists(filePath))
        {
            var content = await File.ReadAllTextAsync(filePath);
            if (content.Length > 3800)
                content = content[..3800] + "\n\n... (truncated)";

            await SendReplyAsync(botClient, chatId, content, ct);
        }
        else
        {
            await botClient.SendMessage(chatId, $"Guida '{guideName}' non trovata.", cancellationToken: ct);
        }
    }

    private async Task HandleGuideUpdateAsync(string chatId, string guideName, string description,
        ITelegramBotClient botClient, CancellationToken ct)
    {
        var filePath = Path.Combine("guides", $"{guideName}.md");
        var currentContent = File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : "# " + guideName;

        var diffPrompt = $"Genera un unified diff per questa modifica alla guida '{guideName}':\n{description}\n\nContenuto attuale:\n{currentContent}";
        var diff = await _llmService.GenerateAsync(diffPrompt, "Sei un assistente che genera diff testuali.");

        await SendReplyAsync(botClient, chatId,
            $"Proposta modifica per '{guideName}.md':\n\n```\n{diff}\n```", ct);
    }

    private async Task HandlePublishAsync(string chatId, string guideName,
        ITelegramBotClient botClient, CancellationToken ct)
    {
        await botClient.SendMessage(chatId,
            $"Publish della guida '{guideName}' da staging a main.\nMerge in corso...",
            ParseMode.Markdown, cancellationToken: ct);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception ex, CancellationToken ct)
    {
        _logger.LogError(ex, "Errore Telegram Bot");
        return Task.CompletedTask;
    }

    public async Task SendMessageAsync(string chatId, string text, CancellationToken ct = default)
    {
        if (_botClient != null)
        {
            await _botClient.SendMessage(chatId, text, cancellationToken: ct);
        }
        else
        {
            _logger.LogInformation("[SIMULATO Telegram] Invio messaggio a {ChatId}: {Text}", chatId, text);
        }
    }
}

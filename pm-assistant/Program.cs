using PmAssistant.Services;
using PmAssistant.Services.Chat;
using PmAssistant.Models.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;

// ── Configuration ─────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables();

// ── LLM Settings ──────────────────────────────────────────────────────────────
var llmSection = builder.Configuration.GetSection("LLM");
builder.Services.Configure<LlmSettings>(llmSection);
builder.Services.AddHttpClient<ILlmService, LlmService>(client =>
{
    client.BaseAddress = new Uri(llmSection["BaseUrl"] ?? "http://localhost:8080");
    client.Timeout = TimeSpan.FromMinutes(5);
});

// ── Google Sheets Settings ────────────────────────────────────────────────────
var sheetsSection = builder.Configuration.GetSection("GoogleSheets");
builder.Services.Configure<GoogleSheetsSettings>(sheetsSection);
builder.Services.AddSingleton<IGoogleSheetsService, GoogleSheetsService>();

// ── Telegram Settings ─────────────────────────────────────────────────────────
var telegramSection = builder.Configuration.GetSection("Telegram");
builder.Services.Configure<TelegramSettings>(telegramSection);
builder.Services.AddSingleton<IAssistantAgentService, AssistantAgentService>();
builder.Services.AddSingleton<ITelegramBotService, TelegramBotService>();
builder.Services.AddHttpClient();

// ── Guide Settings ────────────────────────────────────────────────────────────
var guidesSection = builder.Configuration.GetSection("Guides");
builder.Services.Configure<GuideSettings>(guidesSection);

// ── Reminder Settings ─────────────────────────────────────────────────────────
var reminderSection = builder.Configuration.GetSection("Reminder");
builder.Services.Configure<ReminderSettings>(reminderSection);

// ── Graph Accounts ────────────────────────────────────────────────────────────
builder.Services.Configure<List<GraphAccountConfig>>(builder.Configuration.GetSection("GraphAccounts"));

// ── Google Search Settings ────────────────────────────────────────────────────
var searchSection = builder.Configuration.GetSection("GoogleSearch");

// ── Time Tracking API Settings ────────────────────────────────────────────────
var timeTrackingSection = builder.Configuration.GetSection("TimeTrackingApi");

// ── ToolDispatcher & Workspace ────────────────────────────────────────────────
var workspacePath = Path.Combine(AppContext.BaseDirectory, "AgentWorkspace");
var workspace = new LocalCodeAgent.Core.WorkspaceContext(workspacePath);
builder.Services.AddSingleton(workspace);
builder.Services.AddSingleton<ToolDispatcher>(sp =>
    new ToolDispatcher(
        workspace,
        googleSearchApiKey: searchSection["ApiKey"],
        googleSearchCx: searchSection["Cx"],
        sheetsService: sp.GetService<IGoogleSheetsService>(),
        oreTrackingBaseUrl: timeTrackingSection["BaseUrl"]));

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IGraphAuthService, GraphAuthService>();
builder.Services.AddSingleton<IDailyEntryManager, DailyEntryManager>();
builder.Services.AddSingleton<ITodoQueryService, TodoQueryService>();
builder.Services.AddSingleton<IMonthlyConsolidator, MonthlyConsolidator>();
builder.Services.AddSingleton<IMonthlySummaryService, MonthlySummaryService>();
builder.Services.AddSingleton<IGuideService, GuideService>();
builder.Services.AddSingleton<IPdfParserService, PdfParserService>();
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();

// ── Chat Service ──────────────────────────────────────────────────────────────
var chatSection = builder.Configuration.GetSection("Chat");
builder.Services.Configure<ChatSettings>(chatSection);
builder.Services.AddSingleton<IChatService, ChatService>();

// ── Background Services ───────────────────────────────────────────────────────
builder.Services.AddHostedService<SchedulerHostedService>();

// ── Minimal APIs ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Authentication & CORS ───────────────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultSuperSecretKey123!"))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ── Telegram Bot Startup ──────────────────────────────────────────────────────
using var botScope = app.Services.CreateScope();
var telegramBot = botScope.ServiceProvider.GetRequiredService<ITelegramBotService>();
var telegramSettings = botScope.ServiceProvider.GetRequiredService<IOptions<TelegramSettings>>().Value;

if (!string.IsNullOrEmpty(telegramSettings.BotToken))
{
    try
    {
        var botClient = new TelegramBotClient(telegramSettings.BotToken);

        // Set webhook if configured
        if (!string.IsNullOrEmpty(telegramSettings.WebhookUrl))
        {
            await botClient.SetWebhook(url: telegramSettings.WebhookUrl);
            app.Lifetime.ApplicationStopping.Register(() =>
                botClient.DeleteWebhook().GetAwaiter().GetResult());
        }

        // Start polling in background
        _ = Task.Run(() => telegramBot.StartPollingAsync(app.Lifetime.ApplicationStopping),
            app.Lifetime.ApplicationStopping);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WARN] Telegram bot start failed: {ex.Message}");
    }
}

// ── HTTP Pipeline ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("FrontendCors");

app.UseHttpsRedirection();
app.MapControllers();

// ── Guide API Endpoints ───────────────────────────────────────────────────────
var guideGroup = app.MapGroup("/api/guides");

guideGroup.MapGet("/", async (IGuideService guideService) =>
{
    var guides = await guideService.ListGuidesAsync();
    return Results.Ok(guides);
});

guideGroup.MapGet("/{name}", async (string name, IGuideService guideService) =>
{
    try
    {
        var content = await guideService.GetGuideAsync(name);
        return Results.Ok(new { name, content });
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound($"Guida '{name}' non trovata.");
    }
});

guideGroup.MapPost("/{name}/preview", async (string name, [FromBody] GuideUpdateRequest request, IGuideService guideService) =>
{
    var result = await guideService.PreviewUpdateAsync(name, request.Content);
    return Results.Ok(result);
});

guideGroup.MapPost("/{name}/apply", async (string name, [FromBody] GuideUpdateRequest request, IGuideService guideService) =>
{
    await guideService.ApplyUpdateAsync(name, request.Content);
    return Results.Ok(new { message = $"Guida '{name}' aggiornata in staging." });
});

guideGroup.MapPost("/{name}/publish", async (string name, IGuideService guideService) =>
{
    await guideService.PublishAsync(name);
    return Results.Ok(new { message = $"Guida '{name}' pubblicata su main." });
});

// ── Daily Logs API ────────────────────────────────────────────────────────────
app.MapPost("/api/daily-logs", async (DailyLogRequest request, IDailyEntryManager manager) =>
{
    var entry = new PmAssistant.Models.Domain.DailyLogEntry
    {
        Date = request.Date,
        Client = request.Client,
        Project = request.Project,
        Hours = request.Hours,
        Description = request.Description
    };
    var created = await manager.CreateEntryAsync(entry);
    return Results.Created($"/api/daily-logs/{created.Id}", created);
});

app.MapGet("/api/daily-logs", async (int? year, int? month, IDailyEntryManager manager) =>
{
    if (year.HasValue && month.HasValue)
    {
        var entries = await manager.GetEntriesByMonthAsync(year.Value, month.Value);
        return Results.Ok(entries);
    }
    return Results.BadRequest("Parametri year e month sono obbligatori.");
});

// ── Todos API ─────────────────────────────────────────────────────────────────
app.MapPost("/api/todos", async (TodoRequest request, ITodoQueryService service) =>
{
    await service.AddTodoAsync(request.Title, request.Project, request.DueDate);
    return Results.Ok(new { message = "Task aggiunto." });
});

app.MapGet("/api/todos", async ([FromQuery] string? project, [FromQuery] bool? completed, ITodoQueryService service) =>
{
    var todos = await service.GetTodosAsync(project, completed);
    return Results.Ok(todos);
});

// ── Monthly API ───────────────────────────────────────────────────────────────
app.MapGet("/api/reconcile", async (int year, int month, IMonthlyConsolidator consolidator) =>
{
    var result = await consolidator.ConsolidateAsync(year, month);
    return Results.Ok(new { Year = year, Month = month, Reconciliation = result });
});

app.MapGet("/api/summary", async (int year, int month, IMonthlySummaryService summaryService) =>
{
    var result = await summaryService.GenerateAsync(year, month);
    return Results.Ok(new { Year = year, Month = month, Summary = result });
});

// ── Chat: stesso percorso dei messaggi liberi Telegram (utile per test) ───────
app.MapPost("/api/chat", async (ChatApiRequest request, ITelegramBotService bot) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest("Campo 'message' obbligatorio.");

    try
    {
        var reply = await bot.ProcessMessageAsync(request.Message);
        return Results.Ok(new { reply });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ── Health: gestito da HealthController (GET /api/health) ─────────────────────

// ── Banner & Run ──────────────────────────────────────────────────────────────
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.DarkYellow;
Console.WriteLine("╔══════════════════════════════════════════════╗");
Console.WriteLine($"║  SECRETARY AI ASSISTANT                      ║");
Console.WriteLine($"║  .NET 8 + llama.cpp                          ║");
Console.WriteLine("╚══════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

var configuredUrl = builder.Configuration["Kestrel:Endpoints:Http:Url"];
var port = Uri.TryCreate(configuredUrl, UriKind.Absolute, out var kestrelUri) ? kestrelUri.Port.ToString() : "5000";
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"✓ Server avviato su http://localhost:{port}");
Console.ResetColor();
Console.WriteLine("  Endpoints:");
Console.WriteLine($"    GET /api/health          — Health check");
Console.WriteLine($"    POST /api/daily-logs     — Registra ore");
Console.WriteLine($"    GET  /api/daily-logs?year=&month= — Lista ore mese");
Console.WriteLine($"    POST /api/todos           — Aggiungi task");
Console.WriteLine($"    GET  /api/todos           — Lista tasks");
Console.WriteLine($"    GET  /api/reconcile?year=&month= — Riconciliazione");
Console.WriteLine($"    GET  /api/summary?year=&month=   — Riepilogo Fiscozen");
Console.WriteLine($"    GET  /api/guides          — Lista guide");
Console.WriteLine($"    GET  /api/guides/{{name}} — Leggi guida");
Console.WriteLine();

app.Run();

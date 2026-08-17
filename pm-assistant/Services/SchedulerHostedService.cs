using Microsoft.Extensions.Options;
using PmAssistant.Services;

namespace PmAssistant.Services;

public class ReminderSettings
{
    public bool Enabled { get; set; } = true;
    public string Time { get; set; } = "18:00";
    public string TimeZone { get; set; } = "Europe/Rome";
}

public class SchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulerHostedService> _logger;
    private readonly ReminderSettings _reminder;
    private Timer? _timer;

    public SchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SchedulerHostedService> logger,
        IOptions<ReminderSettings> reminder)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _reminder = reminder.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_reminder.Enabled)
        {
            _logger.LogInformation("Reminder disabilitato in configurazione.");
            return Task.CompletedTask;
        }

        _logger.LogInformation("SchedulerHostedService avviato.");
        ScheduleNextCheck();
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        _logger.LogInformation("SchedulerHostedService fermato.");
        return base.StopAsync(cancellationToken);
    }

    private void ScheduleNextCheck()
    {
        if (!TimeSpan.TryParse(_reminder.Time, out var targetTime))
        {
            _logger.LogError("Formato orario reminder non valido: {Time}", _reminder.Time);
            return;
        }

        var now = DateTime.Now;
        var todayTarget = new DateTime(now.Year, now.Month, now.Day,
            targetTime.Hours, targetTime.Minutes, 0);

        if (now >= todayTarget)
            todayTarget = todayTarget.AddDays(1);

        var due = (todayTarget - now).TotalMilliseconds;
        _logger.LogInformation("Prossimo reminder tra {Minutes} minuti ({NextAt})",
            due / 60000, todayTarget.ToString("HH:mm"));

        _timer = new Timer(async _ => await TriggerReminderAsync(), null,
            TimeSpan.FromMilliseconds(due), Timeout.InfiniteTimeSpan);
    }

    private async Task TriggerReminderAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var todosService = scope.ServiceProvider.GetRequiredService<ITodoQueryService>();

            var pendingTodos = await todosService.GetTodosAsync(completed: false);
            var dueToday = pendingTodos.Where(t =>
                !string.IsNullOrEmpty(t.DueDate) &&
                DateTime.TryParse(t.DueDate, out var dueDate) &&
                (dueDate.Date <= DateTime.Today)).ToList();

            if (dueToday.Any())
            {
                _logger.LogInformation("Reminder: {Count} task in scadenza.", dueToday.Count);
                foreach (var todo in dueToday)
                {
                    _logger.LogInformation("  - {Title} (scadenza: {DueDate})", todo.Title, todo.DueDate);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il reminder");
        }
        finally
        {
            ScheduleNextCheck();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Il timer gestisce la logica; questo metodo resta in vita finché l'app è attiva
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}

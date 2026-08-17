namespace PmAssistant.Models.Dtos;

public class DailyLogRequest
{
    public DateTime Date { get; set; }
    public string Client { get; set; } = "";
    public string Project { get; set; } = "";
    public decimal Hours { get; set; }
    public string Description { get; set; } = "";
    public List<TaskRequest>? Tasks { get; set; }
}

public class TaskRequest
{
    public string Description { get; set; } = "";
    public bool Completed { get; set; }
    public string? DueDate { get; set; }
    public string Project { get; set; } = "";
}

public class TodoRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Project { get; set; } = "";
    public string? DueDate { get; set; }
}

public class GuideUpdateRequest
{
    public string GuideName { get; set; } = "";
    public string Content { get; set; } = "";
    public string Diff { get; set; } = "";
}

public class EmailDraftResponse
{
    public string Id { get; set; } = "";
    public string Account { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public string Subject { get; set; } = "";
    public string BodyPreview { get; set; } = "";
    public string Status { get; set; } = "";
}

public class MonthlySummaryRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
}

public class ChatApiRequest
{
    public string Message { get; set; } = "";
}

public class HealthStatusResponse
{
    public bool LlmOnline { get; set; }
    public string LlmModel { get; set; } = "";
    public bool TelegramConnected { get; set; }
    public DateTime Uptime { get; set; }
}

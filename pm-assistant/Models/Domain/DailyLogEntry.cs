namespace PmAssistant.Models.Domain;

public class DailyLogEntry
{
    public string Id { get; set; } = "";
    public DateTime Date { get; set; }
    public string Client { get; set; } = "";
    public string Project { get; set; } = "";
    public decimal Hours { get; set; }
    public string Description { get; set; } = "";
    public List<TaskItem> Tasks { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TaskItem
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Completed { get; set; }
    public DateTime? DueDate { get; set; }
    public string Project { get; set; } = "";
}

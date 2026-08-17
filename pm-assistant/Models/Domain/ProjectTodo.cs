namespace PmAssistant.Models.Domain;

public class ProjectTodo
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Completed { get; set; }
    public string Project { get; set; } = "";
    public string? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

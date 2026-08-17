using PmAssistant.Models.Domain;
using PmAssistant.Services;

namespace PmAssistant.Services;

public interface ITodoQueryService
{
    Task AddTodoAsync(string title, string project = "", string? dueDate = null);
    Task<List<ProjectTodo>> GetTodosAsync(string? project = null, bool? completed = null);
    Task ToggleTodoAsync(string todoId);
}

public class TodoQueryService : ITodoQueryService
{
    private readonly IGoogleSheetsService _sheets;
    private readonly IAuditLogService _audit;

    public TodoQueryService(IGoogleSheetsService sheets, IAuditLogService audit)
    {
        _sheets = sheets;
        _audit = audit;
    }

    public async Task AddTodoAsync(string title, string project = "", string? dueDate = null)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        await _sheets.AppendRowsAsync(
            "Todos",
            id, title, false, project ?? "", dueDate ?? "", DateTime.UtcNow.ToString("o"));

        await _audit.LogAsync("todo_add", $"Task aggiunto: {title}");
    }

    public async Task<List<ProjectTodo>> GetTodosAsync(string? project = null, bool? completed = null)
    {
        var rows = await _sheets.ReadRowsAsync("Todos");
        var todos = new List<ProjectTodo>();

        foreach (var row in rows.Skip(1))
        {
            if (row.Count < 3) continue;

            var todoCompleted = string.Equals(row[2]?.ToString(), "TRUE", StringComparison.OrdinalIgnoreCase);
            if (completed.HasValue && todoCompleted != completed.Value) continue;
            if (!string.IsNullOrEmpty(project) && row[3]?.ToString() != project) continue;

            todos.Add(new ProjectTodo
            {
                Id = row[0]?.ToString() ?? "",
                Title = row[1]?.ToString() ?? "",
                Completed = todoCompleted,
                Project = row.Count > 3 ? (row[3]?.ToString() ?? "") : "",
                DueDate = row.Count > 4 && !string.IsNullOrEmpty(row[4]?.ToString()) ? row[4]?.ToString() : null,
                CreatedAt = DateTime.UtcNow
            });
        }

        return todos;
    }

    public async Task ToggleTodoAsync(string todoId)
    {
        var rows = await _sheets.ReadRowsAsync("Todos");
        for (var i = 1; i < rows.Count; i++)
        {
            if (rows[i][0]?.ToString() == todoId)
            {
                var currentCompleted = string.Equals(rows[i][2]?.ToString(), "TRUE", StringComparison.OrdinalIgnoreCase);
                // Update via append with marker
                await _sheets.AppendRowsAsync(
                    "Todos",
                    todoId, $"[UPDATED]", !currentCompleted,
                    rows[i].Count > 3 ? (rows[i][3]?.ToString() ?? "") : "",
                    rows[i].Count > 4 ? (rows[i][4]?.ToString() ?? "") : "",
                    DateTime.UtcNow.ToString("o"));

                await _audit.LogAsync("todo_toggle", $"Task {todoId} toggled: {!currentCompleted}");
                break;
            }
        }
    }
}

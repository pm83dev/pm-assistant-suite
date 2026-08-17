namespace PmAssistant.Models.Domain;

public class EmailMessage
{
    public string Id { get; set; } = "";
    public string Subject { get; set; } = "";
    public string BodyPreview { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public DateTime ReceivedTime { get; set; }
    public string AccountName { get; set; } = "";
    public bool IsRead { get; set; }
}

public class DraftEmail
{
    public string Id { get; set; } = "";
    public string Account { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public string Subject { get; set; } = "";
    public string BodyDraft { get; set; } = "";
    public EmailDraftStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? OriginalMessageId { get; set; }
}

public enum EmailDraftStatus
{
    Pending,
    Approved,
    Rejected,
    Sent
}

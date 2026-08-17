namespace PmAssistant.Models.Domain;

public class ClientInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? VatNumber { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

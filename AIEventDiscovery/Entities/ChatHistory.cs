namespace AIEventDiscovery.Entities;

public class ChatHistory : BaseEntity
{
    public Guid UserId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;

    // Navigation property
    public User User { get; set; } = null!;
}

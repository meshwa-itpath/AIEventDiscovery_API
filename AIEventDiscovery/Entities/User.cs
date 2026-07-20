namespace AIEventDiscovery.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsOnBoardingCompleted { get; set; } = false;
    public string? Role { get; set; }
    public string? Technology { get; set; }

    public ICollection<ChatHistory> ChatHistories { get; set; } = new List<ChatHistory>();
}

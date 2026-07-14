using System;

namespace AIEventDiscovery.Entities
{
    public class ChatHistory
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}

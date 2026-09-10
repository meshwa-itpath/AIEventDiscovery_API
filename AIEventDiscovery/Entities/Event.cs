using Pgvector;

namespace AIEventDiscovery.Entities
{
    public class Event : BaseEntity
    {
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string? Category { get; set; }
        public string? SubCategory { get; set; }
        public List<string> Technologies { get; set; } = [];
        public List<string> Tags { get; set; } = [];
        public string? Organizer { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Venue { get; set; }
        public string? Mode { get; set; }
        public string? Level { get; set; }
        public string? EventType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? Rating { get; set; }

        // pgvector: 384-dimensional embedding from all-MiniLM-L6-v2
        public Vector? Embedding { get; set; }
    }
}

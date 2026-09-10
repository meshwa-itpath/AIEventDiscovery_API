using AIEventDiscovery.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIEventDiscovery.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ChatHistory> ChatHistories { get; set; }
        public DbSet<Event> Events { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensures the pgvector extension is created automatically during migrations
            modelBuilder.HasPostgresExtension("vector");

            // Apply configurations dynamically
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}

using AIEventDiscovery.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIEventDiscovery.Data.Configurations
{
    public class EventConfiguration : BaseEntityConfiguration<Event>
    {
        public override void Configure(EntityTypeBuilder<Event> builder)
        {
            base.Configure(builder);

            builder.Property(e => e.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(e => e.Description)
                   .IsRequired()
                   .HasMaxLength(2000);

            builder.Property(e => e.Technologies)
                   .HasColumnType("text[]");

            builder.Property(e => e.Tags)
                   .HasColumnType("text[]");

            // 384-dimensional vector column — matches all-MiniLM-L6-v2 (SmartComponents.LocalEmbeddings)
            builder.Property(e => e.Embedding)
                   .HasColumnType("vector(384)");

            // HNSW index for fast approximate nearest-neighbour search using cosine distance
            builder.HasIndex(e => e.Embedding)
                   .HasMethod("hnsw")
                   .HasOperators("vector_cosine_ops");
        }
    }
}

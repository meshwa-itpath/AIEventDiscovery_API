using AIEventDiscovery.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIEventDiscovery.Data.Configurations
{
    public class ChatHistoryConfiguration : IEntityTypeConfiguration<ChatHistory>
    {
        public void Configure(EntityTypeBuilder<ChatHistory> builder)
        {
            builder.HasKey(ch => ch.Id);
            
            builder.Property(ch => ch.Question).IsRequired();
            builder.Property(ch => ch.Response).IsRequired();
        }
    }
}

using AIEventDiscovery.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIEventDiscovery.Data.Configurations
{
    public class ChatHistoryConfiguration : BaseEntityConfiguration<ChatHistory>
    {
        public override void Configure(EntityTypeBuilder<ChatHistory> builder)
        {
            base.Configure(builder);

            builder.Property(ch => ch.Question)
                   .IsRequired();

            builder.Property(ch => ch.Response)
                   .IsRequired();
        }
    }
}

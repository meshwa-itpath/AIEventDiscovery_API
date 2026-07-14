using AIEventDiscovery.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIEventDiscovery.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
            builder.Property(u => u.PasswordHash).IsRequired();
            
            builder.HasIndex(u => u.Email).IsUnique();

            builder.HasMany(u => u.ChatHistories)
                   .WithOne(ch => ch.User)
                   .HasForeignKey(ch => ch.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

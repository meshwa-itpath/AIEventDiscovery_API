using AIEventDiscovery.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIEventDiscovery.Data.Configurations;

/// <summary>
/// Shared EF Core configuration applied to ALL entities that inherit BaseEntity.
/// This is the single place that configures Id and CreatedAt generation — at the DATABASE level.
/// 
/// Each entity configuration (e.g. UserConfiguration) calls base.Configure(builder)
/// to inherit these settings automatically.
/// </summary>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
               .HasDefaultValueSql("gen_random_uuid()")  
               .ValueGeneratedOnAdd();                  

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("now()")  
               .ValueGeneratedOnAdd();      

        builder.Property(e => e.IsDeleted)
               .HasDefaultValue(false)
               .IsRequired();
    }
}

namespace AIEventDiscovery.Entities;

/// <summary>
/// Base class for all database entities.
/// Provides common audit fields that every table should have.
/// All entities must inherit from this instead of defining these fields manually.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } 
    public DateTime CreatedAt { get; set; } 
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
}

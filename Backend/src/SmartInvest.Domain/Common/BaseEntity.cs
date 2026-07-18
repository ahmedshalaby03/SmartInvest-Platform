namespace SmartInvest.Domain.Common;

/// <summary>
/// Base class for all domain entities. Holds the primary key and audit fields.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
}

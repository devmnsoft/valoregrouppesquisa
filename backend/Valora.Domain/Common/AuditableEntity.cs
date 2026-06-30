namespace Valora.Domain.Common;

public abstract record AuditableEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public Guid? UpdatedBy { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime? DeletedAt { get; init; }
    public Guid? DeletedBy { get; init; }
}

namespace Valora.Domain.Entities;

public sealed record Campaign {
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public Guid SurveyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Audience { get; init; }
    public int ResponseGoal { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public string? PublicTokenHash { get; init; }
    public string Status { get; init; } = "draft";
    public Guid? CreatedBy { get; init; }
    public long? ReferenceCode { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; init; }
}

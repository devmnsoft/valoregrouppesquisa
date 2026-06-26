namespace Valora.Domain.Entities;

public sealed record SurveyLink
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

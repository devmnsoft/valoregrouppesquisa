namespace Valora.Domain.Entities;

public sealed record SurveyResponse
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

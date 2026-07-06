namespace Valora.Domain.Entities;

public sealed record ResultRecommendation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ResultScoreId { get; init; }
    public string Band { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}

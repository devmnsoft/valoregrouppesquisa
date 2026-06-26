namespace Valora.Domain.Entities;

public sealed record QuestionOption
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

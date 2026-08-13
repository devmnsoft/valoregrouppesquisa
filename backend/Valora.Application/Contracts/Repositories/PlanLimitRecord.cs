using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public sealed class PlanLimitRecord
{
    public string? LimitKey { get; init; }
    public int? LimitValue { get; init; }
    public string? Period { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid Id { get; init; }
}

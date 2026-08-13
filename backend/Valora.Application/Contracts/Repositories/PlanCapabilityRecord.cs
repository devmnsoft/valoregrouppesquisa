using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public sealed class PlanCapabilityRecord
{
    public string? CapabilityKey { get; init; }
    public bool Enabled { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid Id { get; init; }
}

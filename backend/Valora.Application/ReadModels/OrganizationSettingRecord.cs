namespace Valora.Application.ReadModels;

public sealed class OrganizationSettingRecord
{
    public Guid Id { get; init; }
    public string Settings { get; init; } = "{}";
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

namespace Valora.Application.ReadModels;

public sealed record OrganizationSettingRecord(Guid Id, string Settings, DateTimeOffset? UpdatedAt);

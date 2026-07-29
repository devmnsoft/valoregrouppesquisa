namespace Valora.Application.ReadModels;

public sealed record OrganizationUsageRecord(string MetricKey, decimal MetricValue, DateOnly PeriodMonth, DateTimeOffset? UpdatedAt);

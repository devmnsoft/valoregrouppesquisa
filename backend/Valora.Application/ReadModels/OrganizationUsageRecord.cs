namespace Valora.Application.ReadModels;

public sealed record OrganizationUsageRecord(
    string Key,
    string Period,
    long Consumed,
    long Reserved,
    int? Limit,
    long? Available,
    decimal? Percentage,
    bool Unlimited);

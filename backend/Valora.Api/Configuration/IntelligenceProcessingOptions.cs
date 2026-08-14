namespace Valora.Api.Configuration;
public sealed class IntelligenceProcessingOptions
{
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 10;
    public int MaxConcurrentJobs { get; set; } = 1;
    public int MaxAttempts { get; set; } = 3;
}

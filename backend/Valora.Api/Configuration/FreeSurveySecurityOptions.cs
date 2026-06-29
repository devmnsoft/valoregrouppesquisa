namespace Valora.Api.Configuration;

public sealed class FreeSurveySecurityOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxSubmissionsPerIpPerHour { get; set; } = 20;
    public int MaxSubmissionsPerEmailPerDay { get; set; } = 3;
    public int MaxSubmissionsPerTokenPerHour { get; set; } = 30;
    public int DuplicateSubmissionWindowSeconds { get; set; } = 60;
    public int MinSecondsToSubmit { get; set; } = 20;
    public bool EnableHoneypot { get; set; } = true;
}

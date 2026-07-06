namespace Valora.Domain.Entities;

public sealed class LgpdConsent
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? SurveyId { get; set; }
    public Guid? ResponseId { get; set; }
    public string ParticipantEmailHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ConsentText { get; set; } = string.Empty;
    public string ConsentVersion { get; set; } = "v1";
    public bool Accepted { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public string? IpHash { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

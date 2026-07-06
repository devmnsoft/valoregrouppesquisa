namespace Valora.Domain.Entities;

public sealed class GeneratedReport
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? SurveyId { get; set; }
    public Guid? ResponseId { get; set; }
    public Guid? ReportDefinitionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Format { get; set; } = "html";
    public string Status { get; set; } = "generated";
    public Guid? GeneratedBy { get; set; }
    public DateTimeOffset? GeneratedAt { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

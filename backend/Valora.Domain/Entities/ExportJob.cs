namespace Valora.Domain.Entities;

public sealed class ExportJob
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? RequestedBy { get; set; }
    public string Entity { get; set; } = "responses";
    public string Format { get; set; } = "csv";
    public string Status { get; set; } = "queued";
    public string? FilterJson { get; set; }
    public string? ResultFileName { get; set; }
    public string? ResultMimeType { get; set; }
    public string? ResultPayload { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

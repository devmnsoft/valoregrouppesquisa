namespace Valora.Domain.Entities;

public sealed class ReportDefinition
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ReportType { get; set; } = "response";
    public string RequiredModuleCode { get; set; } = "relatorios";
    public string? MinimumPlanCode { get; set; }
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

namespace Valora.Web.Models.ViewModels;

public sealed record IntelligenceWorkspaceViewModel(
    string Slug,
    string Title,
    string Eyebrow,
    string Purpose,
    string Endpoint,
    string ItemLabel,
    string[] Filters,
    string[] DetailFields,
    string Limitation,
    string? Notice = null,
    string? PrimaryAction = null);

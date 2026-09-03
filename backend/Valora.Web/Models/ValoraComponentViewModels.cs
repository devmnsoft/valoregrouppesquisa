using System.ComponentModel.DataAnnotations;

namespace Valora.Web.Models;

public sealed record PageHelpPanelViewModel(string Title, string Description, IReadOnlyList<string>? Steps = null, bool Collapsed = false);
public sealed record MetricCardViewModel(string Label, string Value, string? Hint = null, string? Tone = null);
public sealed record EmptyStateViewModel(string Title, string Description, string? ActionLabel = null, string? ActionHref = null);
public sealed record StateViewModel(string Title, string Description, string? RetryLabel = null);
public sealed record StatusBadgeViewModel(string Label, string? Tone = null);
public sealed record FormSectionViewModel(string Title, string? Description = null);
public sealed record FormActionsViewModel(string SubmitLabel = "Salvar", string CancelLabel = "Cancelar", string? CancelHref = null, bool Critical = false);
public sealed record SearchBoxViewModel(string Name = "search", string Label = "Buscar", string Placeholder = "Buscar registros", string? Value = null);
public sealed record FilterOptionViewModel(string Value, string Label);
public sealed record FilterBarViewModel(SearchBoxViewModel Search, string StatusLabel = "Status", IReadOnlyList<FilterOptionViewModel>? Statuses = null);
public sealed record DataTableViewModel(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows, string EmptyMessage = "Nenhum registro encontrado para os filtros aplicados.");
public sealed record ActionCardViewModel(string Title, string Description, string ActionLabel, string ActionHref, string? Eyebrow = null, string? Icon = null);
public sealed record FeatureCardViewModel(string Title, string Description, string? Href = null, string? ActionLabel = null, string? Icon = null, string? Status = null);
public sealed record LoadingButtonViewModel(string Label, string LoadingLabel = "Processando…", string Style = "primary", string Type = "submit", string? DataAction = null, bool Disabled = false);
public sealed record InsightCardViewModel(string Title, string Description, string? Evidence = null, string Tone = "info");
public sealed record EvidenceCardViewModel(string Title, string Description, string? Source = null, string? Confidence = null);
public sealed record AlertBannerViewModel(string Title, string Message, string Tone = "info", bool Dismissible = false);
public sealed record FormShellViewModel(string Title, string? Description = null, string? Id = null);
public sealed record FieldHintViewModel(string Id, string Text);
public sealed record ValidationSummaryViewModel(string Message = "Revise os campos destacados antes de continuar.");
public sealed record ResponsiveTabViewModel(string Label, string Href, bool Active = false, string? Count = null);
public sealed record ResponsiveTabsViewModel(string Label, IReadOnlyList<ResponsiveTabViewModel> Items);
public sealed record PageToolbarAction(string Label, string? Href = null, string Style = "ghost", string? DataAction = null);
public sealed record PageToolbarViewModel(string? Label = null, IReadOnlyList<PageToolbarAction>? Actions = null);

public sealed class ResultDetailsViewModel
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    [RegularExpression("^[A-Za-z0-9_-]+$")]
    public required string ResponseId { get; init; }
}

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

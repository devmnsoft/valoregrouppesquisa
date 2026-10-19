using Valora.Application.ReadModels;

namespace Valora.Web.Models.ViewModels;

public sealed record ResponseItemViewModel(
    Guid Id,
    string SurveyTitle,
    string FormName,
    string ParticipantName,
    string ParticipantEmail,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt) {

    public string StatusBadgeClass => Status?.ToLowerInvariant() switch {
        "completed" => "text-bg-success",
        "pending" => "text-bg-warning text-dark",
        "processing" => "text-bg-info text-white",
        _ => "text-bg-secondary"
    };

    public string StatusLabel => Status?.ToLowerInvariant() switch {
        "completed" => "Concluída",
        "pending" => "Em Andamento",
        "processing" => "Em Processamento",
        _ => Status ?? "Indefinido"
    };
}

public sealed class ResponsesIndexViewModel {
    public ResponsesIndexViewModel(IReadOnlyList<ResponseItemViewModel> items, string? search = null) {
        Items = items;
        Search = search;
        TotalCount = items.Count;
        CompletedCount = items.Count(i => string.Equals(i.Status, "completed", StringComparison.OrdinalIgnoreCase));
        SurveysCount = items.Select(i => i.SurveyTitle).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    }

    public IReadOnlyList<ResponseItemViewModel> Items { get; }
    public string? Search { get; }
    public int TotalCount { get; }
    public int CompletedCount { get; }
    public int SurveysCount { get; }
}

public sealed class ResponseDetailViewModel {
    public ResponseDetailViewModel(AdminResultReadModel result) {
        Result = result;
    }

    public AdminResultReadModel Result { get; }
}

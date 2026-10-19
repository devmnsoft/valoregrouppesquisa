using Valora.Application.DTOs;

namespace Valora.Web.Models.ViewModels;

public sealed class CommunicationsIndexViewModel {
    public CommunicationsIndexViewModel(
        EmailStatusDto status,
        IReadOnlyList<EmailJobDto> jobs,
        IReadOnlyList<EmailTemplateDto> templates,
        string? statusFilter = null,
        string? search = null) {
        Status = status;
        Jobs = jobs;
        Templates = templates;
        StatusFilter = statusFilter;
        Search = search;
    }

    public EmailStatusDto Status { get; }
    public IReadOnlyList<EmailJobDto> Jobs { get; }
    public IReadOnlyList<EmailTemplateDto> Templates { get; }
    public string? StatusFilter { get; }
    public string? Search { get; }

    public int TotalDispatches => Status.Sent + Status.Queued + Status.Processing + Status.Failed + Status.DeadLetter;
    public int InQueueCount => Status.Queued + Status.Processing;
    public int IssuesCount => Status.Failed + Status.DeadLetter;

    public static string FormatStatusBadge(string? status) => status?.ToLowerInvariant() switch {
        "sent" => "text-bg-success",
        "queued" => "text-bg-warning text-dark",
        "processing" => "text-bg-info text-white",
        "failed" => "text-bg-danger",
        "dead_letter" => "text-bg-dark",
        _ => "text-bg-secondary"
    };

    public static string FormatStatusLabel(string? status) => status?.ToLowerInvariant() switch {
        "sent" => "Entregue",
        "queued" => "Na fila",
        "processing" => "Processando",
        "failed" => "Falha temporária",
        "dead_letter" => "Quarentena (Dead Letter)",
        _ => status ?? "Indefinido"
    };
}

using Valora.Application.DTOs;

namespace Valora.Web.Models.ViewModels;

public sealed record ExportEntityOption(
    string Key,
    string Title,
    string Description,
    string Icon,
    IReadOnlyList<string> Formats);

public sealed class ExportsIndexViewModel {
    public ExportsIndexViewModel(IReadOnlyList<ExportJobDto> jobs) {
        Jobs = jobs;
        TotalCount = jobs.Count;
        CompletedCount = jobs.Count(j => string.Equals(j.Status, "completed", StringComparison.OrdinalIgnoreCase));
        ProcessingCount = jobs.Count(j => string.Equals(j.Status, "processing", StringComparison.OrdinalIgnoreCase) || string.Equals(j.Status, "pending", StringComparison.OrdinalIgnoreCase));
        FailedCount = jobs.Count(j => string.Equals(j.Status, "failed", StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<ExportJobDto> Jobs { get; }
    public int TotalCount { get; }
    public int CompletedCount { get; }
    public int ProcessingCount { get; }
    public int FailedCount { get; }

    public IReadOnlyList<ExportEntityOption> AvailableEntities { get; } = [
        new("responses", "Respostas da Coleta", "Respostas individuais e agregadas dos instrumentos de pesquisa e diagnósticos.", "bi-chat-square-text", ["csv", "xlsx", "json"]),
        new("results", "Resultados & Indicadores", "Métricas consolidadas, scores por dimensão e resumos executivos.", "bi-graph-up-arrow", ["csv", "xlsx", "pdf"]),
        new("audit", "Trilha de Auditoria", "Registro de eventos de governança, acessos e segurança da organização.", "bi-shield-check", ["csv", "json"]),
        new("surveys", "Pesquisas & Campanhas", "Cadastro de pesquisas, status de ativação, metadados e público.", "bi-journal-check", ["csv", "json"]),
        new("forms", "Formulários & Estruturas", "Estruturas de perguntas, blocos temáticos e regras de validação.", "bi-ui-checks", ["csv", "json"])
    ];
}

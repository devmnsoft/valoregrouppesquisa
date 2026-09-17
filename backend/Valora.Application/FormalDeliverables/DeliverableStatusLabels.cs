namespace Valora.Application.FormalDeliverables;

public static class DeliverableStatusLabels {
    public static string Editorial(string? value) => value switch {
        DeliverableEditorialStatuses.Draft => "Rascunho",
        DeliverableEditorialStatuses.InReview => "Em revisão",
        DeliverableEditorialStatuses.Published => "Publicado",
        DeliverableEditorialStatuses.Superseded => "Substituído",
        null or "" => "Não informada",
        _ => value
    };

    public static string Processing(string? value) => value switch {
        DeliverableProcessingStatuses.AwaitingGeneration => "Aguardando geração",
        DeliverableProcessingStatuses.Processing => "Processando",
        DeliverableProcessingStatuses.Available => "Arquivo disponível",
        DeliverableProcessingStatuses.Failed => "Falha na geração",
        null or "" => "Não informado",
        _ => value
    };

    public static string Type(string? value) => value switch {
        DeliverableTypes.ExecutiveReport => "Relatório executivo",
        DeliverableTypes.Certificate => "Certificado",
        null or "" => "Entregável",
        _ => value
    };

    public static string Section(string? value) => value switch {
        "objective" => "Objetivo",
        "period" => "Período",
        "participation" => "Participação",
        "results" => "Resultados",
        "dimensions" => "Dimensões",
        "evidence" => "Evidências",
        "limitations" => "Limitações",
        "recommendations" => "Recomendações",
        "action_plans" => "Planos de ação",
        null or "" => "Seção",
        _ => value
    };

    public static string ShareStatus(string? value) => value switch {
        "active" => "Ativo",
        "revoked" => "Revogado",
        "expired" => "Expirado",
        null or "" => "Indefinido",
        _ => value
    };

    public static string EditorialTone(string? value) => value switch {
        DeliverableEditorialStatuses.Published => "success",
        DeliverableEditorialStatuses.InReview => "warning",
        DeliverableEditorialStatuses.Superseded => "neutral",
        _ => "neutral"
    };

    public static string ProcessingTone(string? value) => value switch {
        DeliverableProcessingStatuses.Available => "success",
        DeliverableProcessingStatuses.Processing => "warning",
        DeliverableProcessingStatuses.Failed => "danger",
        _ => "neutral"
    };
}

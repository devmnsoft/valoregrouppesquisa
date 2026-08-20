namespace Valora.Application.OrganizationalIntelligence;

/// <summary>Deterministic, evidence-first read engine. It never completes absent dimensions with synthetic values.</summary>
public sealed class ValoraIntelligenceEngine : IValoraIntelligenceEngine
{
    private static readonly string[] ReportSections = ["Resumo Executivo", "Leitura Estratégica", "Situação Atual",
        "Índice Geral", "Índices por Dimensão", "Análise das Evidências", "Riscos Organizacionais",
        "Oportunidades", "Pontos Fortes", "Fragilidades", "Comparativos", "Prioridades",
        "Plano Estratégico Recomendado", "Indicadores", "Anexos"];

    public OrganizationalDiagnosisSummary Analyze(Guid organizationId, Guid? surveyId,
        IEnumerable<DiagnosisEvidence> evidence, DateTime? createdAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;
        var valid = evidence.Where(x => x.OrganizationId == organizationId &&
            (!surveyId.HasValue || x.SurveyId == surveyId) && x.NormalizedScore is >= 0 and <= 100 &&
            x.Weight is > 0 and <= 100 && !string.IsNullOrWhiteSpace(x.Dimension)).ToList();
        var dimensions = valid.GroupBy(x => x.Dimension.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => Dimension(organizationId, surveyId, group.Key, group.ToList(), now))
            .OrderBy(x => OfficialOrder(x.Dimension)).ThenBy(x => x.Dimension).ToList();
        var allIds = valid.Select(x => x.Id).Distinct().ToList();
        decimal? score = valid.Count == 0 ? null : Math.Round(valid.Sum(x => x.NormalizedScore!.Value * x.Weight) / valid.Sum(x => x.Weight), 2);
        var confidence = Confidence(allIds.Count);
        var risks = dimensions.Where(x => x.Score < 50).Select(x => new OrganizationalRisk(Guid.NewGuid(), organizationId,
            surveyId, x.Dimension, x.Concept, x.Score!.Value, x.MaturityLevel, x.ConfidenceLevel, x.Evidence,
            x.Interpretation, x.Risk!, x.Recommendation!, x.Priority, now)).ToList();
        var fragilities = risks.Select(x => new OrganizationalFragility(Guid.NewGuid(), organizationId, surveyId,
            x.Dimension, x.Concept, x.Score, x.MaturityLevel, x.ConfidenceLevel, x.Evidence, x.Interpretation,
            x.Risk, x.Recommendation, x.Priority, now)).ToList();
        var strengths = dimensions.Where(x => x.Score >= 75).Select(x => new OrganizationalStrength(Guid.NewGuid(),
            organizationId, surveyId, x.Dimension, x.Concept, x.Score!.Value, x.MaturityLevel, x.ConfidenceLevel,
            x.Evidence, x.Interpretation, null, null, "monitor", now)).ToList();
        var opportunities = dimensions.Where(x => x.Score is >= 50 and < 75).Select(x => new OrganizationalOpportunity(
            Guid.NewGuid(), organizationId, surveyId, x.Dimension, x.Concept, x.Score!.Value, x.MaturityLevel,
            x.ConfidenceLevel, x.Evidence, x.Interpretation, x.Risk, x.Recommendation!, x.Priority, now)).ToList();
        var priorities = risks.OrderBy(x => x.Score).Select(x => new RecommendedPriority(Guid.NewGuid(), organizationId,
            surveyId, x.Dimension, x.Concept, x.Score, x.MaturityLevel, x.ConfidenceLevel, x.Evidence,
            x.Interpretation, x.Risk, x.Recommendation, x.Priority, now)).ToList();
        var insights = risks.Select(x => Insight(x, now)).ToList();
        var actions = insights.Select(x => Action(x, now)).ToList();
        var heatmap = dimensions.Select(x => new HeatmapCell(Guid.NewGuid(), organizationId, surveyId, x.Dimension,
            x.Concept, x.Score, x.MaturityLevel, x.ConfidenceLevel, x.Evidence, x.Interpretation, x.Risk,
            x.Recommendation, x.Priority, now, null, x.Score < 50 ? "critical" : x.Score < 75 ? "attention" : "healthy")).ToList();
        var radar = ValoraOfficialDimensions.All.Select(name => dimensions.FirstOrDefault(x => Equivalent(x.Dimension, name)) is { } d
            ? new RadarDimension(Guid.NewGuid(), organizationId, surveyId, name, d.Concept, d.Score, d.MaturityLevel,
                d.ConfidenceLevel, d.Evidence, d.Interpretation, d.Risk, d.Recommendation, d.Priority, now, true,
                d.Score < 50 ? "Gargalo sistêmico a validar pelas evidências relacionadas." : "Sem efeito cascata crítico demonstrado na amostra.")
            : new RadarDimension(Guid.NewGuid(), organizationId, surveyId, name, "not_observed", null, "não avaliado",
                "low", [], "Não há evidências para esta dimensão; nenhum valor foi estimado.", null, null, "collect_evidence", now, true,
                "Efeito sistêmico não avaliável sem evidências.")).ToList();
        var interpretation = score is null ? "Não há evidências válidas para produzir diagnóstico. Amplie a coleta e o mapeamento metodológico."
            : $"Índice geral de {score:0.##}% calculado sobre {allIds.Count} evidência(s) válida(s); leia-o com os índices dimensionais e a confiança {confidence}.";
        var report = Report(organizationId, surveyId, score, confidence, allIds, interpretation, dimensions, risks, opportunities, strengths, priorities, now);
        return new(Guid.NewGuid(), organizationId, surveyId, score, Level(score), confidence, allIds, interpretation,
            dimensions, risks, opportunities, strengths, fragilities, priorities, insights, actions, heatmap, radar, report, now);
    }

    private static MaturityDimensionScore Dimension(Guid org, Guid? survey, string name, List<DiagnosisEvidence> items, DateTime now)
    {
        var score = Math.Round(items.Sum(x => x.NormalizedScore!.Value * x.Weight) / items.Sum(x => x.Weight), 2);
        var ids = items.Select(x => x.Id).Distinct().ToList(); var priority = score < 35 ? "critical" : score < 50 ? "high" : score < 75 ? "medium" : "monitor";
        var risk = score < 50 ? $"A baixa consistência observada em {name} pode comprometer capacidades relacionadas; a causa deve ser validada." : null;
        var recommendation = score < 75 ? $"Validar as evidências de {name}, definir responsável e executar uma intervenção mensurável antes do próximo ciclo." : null;
        return new(Guid.NewGuid(), org, survey, name, items.Select(x => x.Concept).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "mapped",
            score, Level(score), Confidence(ids.Count), ids,
            $"{name} registra {score:0.##}% em {ids.Count} evidência(s) válida(s); o resultado indica {Level(score)} e não identifica causa isoladamente.", risk, recommendation, priority, now);
    }

    private static ExecutiveInsight Insight(OrganizationalRisk risk, DateTime now) => new(Guid.NewGuid(), risk.OrganizationId,
        risk.SurveyId, risk.Dimension, risk.Concept, risk.Score, risk.MaturityLevel, risk.ConfidenceLevel, risk.Evidence,
        $"Foi observado desempenho abaixo de 50% em {risk.Dimension}.", risk.Interpretation,
        "A dimensão deve ser correlacionada às demais capacidades avaliadas no mesmo diagnóstico.",
        "Hipótese: práticas estruturais da dimensão não estão operando de forma consistente; requer validação.",
        $"Pode reduzir a previsibilidade das capacidades dependentes de {risk.Dimension}.", risk.Risk,
        risk.Recommendation, risk.Priority, now);

    private static ActionPlanItem Action(ExecutiveInsight insight, DateTime now) => new(Guid.NewGuid(), insight.OrganizationId,
        insight.SurveyId, insight.Dimension, insight.Concept, insight.Score, insight.MaturityLevel, insight.ConfidenceLevel,
        insight.Evidence, insight.Interpretation, insight.Risk, insight.Recommendation, insight.Priority, now,
        $"Aumentar a consistência de {insight.Dimension}", insight.Recommendation, insight.ProbableCause, null, null,
        $"Variação do índice de {insight.Dimension} no próximo ciclo, com as mesmas regras de cálculo.", "recommended", null, null);

    private static ExecutiveReportViewModel Report(Guid org, Guid? survey, decimal? score, string confidence,
        IReadOnlyList<Guid> ids, string interpretation, IReadOnlyList<MaturityDimensionScore> dimensions,
        IReadOnlyList<OrganizationalRisk> risks, IReadOnlyList<OrganizationalOpportunity> opportunities,
        IReadOnlyList<OrganizationalStrength> strengths, IReadOnlyList<RecommendedPriority> priorities, DateTime now)
    {
        string Content(string title) => title switch
        {
            "Resumo Executivo" or "Leitura Estratégica" or "Situação Atual" => interpretation,
            "Índice Geral" => score is null ? "Índice não calculado por ausência de evidências válidas." : $"Índice geral: {score:0.##}%, confiança {confidence}.",
            "Índices por Dimensão" => dimensions.Count == 0 ? "Nenhuma dimensão observada." : string.Join("; ", dimensions.Select(x => $"{x.Dimension}: {x.Score:0.##}% ({x.MaturityLevel})")),
            "Análise das Evidências" => $"{ids.Count} evidência(s) rastreável(is) participaram do cálculo.",
            "Riscos Organizacionais" => risks.Count == 0 ? "Nenhum risco foi derivado da amostra válida." : string.Join("; ", risks.Select(x => x.Risk)),
            "Oportunidades" => opportunities.Count == 0 ? "Nenhuma oportunidade foi derivada da amostra válida." : string.Join("; ", opportunities.Select(x => x.Recommendation)),
            "Pontos Fortes" => strengths.Count == 0 ? "Nenhum ponto forte atingiu o limiar metodológico." : string.Join("; ", strengths.Select(x => x.Dimension)),
            "Fragilidades" => risks.Count == 0 ? "Nenhuma fragilidade atingiu o limiar metodológico." : string.Join("; ", risks.Select(x => x.Dimension)),
            "Prioridades" or "Plano Estratégico Recomendado" => priorities.Count == 0 ? "Nenhuma recomendação foi criada sem evidência." : string.Join("; ", priorities.Select(x => x.Recommendation)),
            "Comparativos" => "Comparativos dependem de outro ciclo ou agrupamento com amostra válida; nenhum valor foi estimado.",
            "Indicadores" => "Acompanhar os mesmos índices e critérios de evidência no próximo ciclo.",
            _ => "As referências permanecem vinculadas às evidências deste diagnóstico."
        };
        return new(Guid.NewGuid(), org, survey, score, Level(score), confidence, ids, interpretation,
            ReportSections.Select((title, i) => new ExecutiveReportSection($"{i + 1:00}", title, Content(title), ids)).ToList(), now);
    }

    private static string Confidence(int count) => count > 6 ? "high" : count >= 4 ? "medium" : "low";
    private static string Level(decimal? score) => score switch { null => "não avaliado", < 35 => "inicial", < 50 => "em estruturação", < 75 => "consistente", _ => "sustentado" };
    private static int OfficialOrder(string name) { var i = ValoraOfficialDimensions.All.ToList().FindIndex(x => Equivalent(x, name)); return i < 0 ? int.MaxValue : i; }
    private static bool Equivalent(string left, string right) => Normalize(left) == Normalize(right);
    private static string Normalize(string value) => new(value.Normalize(System.Text.NormalizationForm.FormD).Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)).Select(char.ToLowerInvariant).ToArray());
}

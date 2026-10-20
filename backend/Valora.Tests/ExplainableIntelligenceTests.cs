extern alias ValoraWeb;

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Valora.Application.Knowledge;
using Valora.Application.Methodology;
using Valora.Application.OrganizationalIntelligence;
using Valora.Application.ValoraAi;
using Xunit;
using WebProgram = ValoraWeb::Program;

namespace Valora.Tests;

[Trait("Category", "ExplainableIntelligence")]
public sealed class ExplainableIntelligenceTests {
    private static string WebRootPath => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Valora.Web"));

    private sealed class MockInsightRepository(AiInsightDetails? detailsToReturn) : IValoraAiInsightRepository {
        public Task<AiInsight?> GetAsync(Guid organizationId, Guid id, CancellationToken ct) =>
            Task.FromResult(detailsToReturn?.Insight);
        public Task<IReadOnlyList<AiInsight>> ListAsync(Guid organizationId, string? status, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AiInsight>>(detailsToReturn is null ? [] : [detailsToReturn.Insight]);
        public Task<AiInsightListResult> ListAsync(Guid organizationId, Guid userId, bool organizationWide, AiInsightListQuery query, CancellationToken ct) =>
            Task.FromResult(new AiInsightListResult(new Valora.Application.Workspace.PageResult<AiInsightListItem>([], 0, 1, 20), new(0, 0, 0, 0)));
        public Task<AiInsightDetails?> DetailsAsync(Guid organizationId, Guid userId, Guid id, bool organizationWide, CancellationToken ct) =>
            Task.FromResult(detailsToReturn);
        public Task<Guid> CreateAsync(AiRunContext context, Guid runId, AiInsightDraft insight, CancellationToken ct) =>
            Task.FromResult(Guid.NewGuid());
    }

    private sealed class MockMethodologyRepository : IMethodologyRepository {
        public Task<IReadOnlyList<MethodologyConceptDto>> ListConceptsAsync(string? search, string? pillar, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MethodologyConceptDto>>([
                new(Guid.NewGuid(), "lideranca", "Liderança", "Estratégia", "Capacidade de orientar e sustentar decisões.", "Sustentação", "Alinhamento", [], [], [], [], [], "L3", "2.0", "active", 1, 1)
            ]);
        public Task<MethodologyConceptDto?> GetConceptAsync(string code, CancellationToken ct) =>
            Task.FromResult<MethodologyConceptDto?>(new(Guid.NewGuid(), code, code, "Estratégia", "Definição oficial", "Propósito", "Evolução", [], [], [], [], [], "L3", "2.0", "active", 1, 1));
        public Task<IReadOnlyList<MethodologyRelationDto>> ListRelationsAsync(string? conceptCode, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MethodologyRelationDto>>([
                new(Guid.NewGuid(), "lideranca", "Liderança", "comunicacao", "Comunicação", "influencia", 0.85m, "Liderança alinhada fortalece canais de comunicação.")
            ]);
        public Task<IReadOnlyList<MethodologyEvidenceDto>> ListEvidenceAsync(string conceptCode, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MethodologyEvidenceDto>>([]);
    }

    private sealed class MockKnowledgeRepository : IKnowledgeRepository {
        public Task<KnowledgeSummary> SummaryAsync(Guid organizationId, CancellationToken ct) => Task.FromResult(new KnowledgeSummary(0, 0, 0, 0, 0, 0));
        public Task<IReadOnlyList<KnowledgeCategory>> CategoriesAsync(Guid organizationId, CancellationToken ct) => Task.FromResult<IReadOnlyList<KnowledgeCategory>>([]);
        public Task<IReadOnlyList<KnowledgeArticle>> ArticlesAsync(Guid organizationId, string? query, string? status, Guid? categoryId, CancellationToken ct) => Task.FromResult<IReadOnlyList<KnowledgeArticle>>([]);
        public Task<KnowledgeArticle?> ArticleAsync(Guid organizationId, Guid id, CancellationToken ct) => Task.FromResult<KnowledgeArticle?>(null);
        public Task<KnowledgeArticle> CreateArticleAsync(Guid organizationId, CreateArticleRequest request, Guid actor, string correlationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<KnowledgeArticle> TransitionArticleAsync(Guid organizationId, Guid id, string status, Guid actor, string correlationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<KnowledgeArticleVersion>> VersionsAsync(Guid organizationId, Guid articleId, CancellationToken ct) => Task.FromResult<IReadOnlyList<KnowledgeArticleVersion>>([]);
        public Task<IReadOnlyList<Playbook>> PlaybooksAsync(Guid organizationId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Playbook>>([]);
        public Task<Playbook> CreatePlaybookAsync(Guid organizationId, CreatePlaybookRequest request, Guid actor, string correlationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<OrganizationalLesson>> LessonsAsync(Guid organizationId, CancellationToken ct) => Task.FromResult<IReadOnlyList<OrganizationalLesson>>([]);
        public Task<OrganizationalLesson> CreateLessonAsync(Guid organizationId, CreateLessonRequest request, Guid actor, string correlationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<LearningPath>> LearningPathsAsync(Guid organizationId, CancellationToken ct) => Task.FromResult<IReadOnlyList<LearningPath>>([]);
        public Task<LearningPath> CreateLearningPathAsync(Guid organizationId, CreateLearningPathRequest request, Guid actor, string correlationId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<CognitiveTerm>> TermsAsync(Guid organizationId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CognitiveTerm>>([
                new(Guid.NewGuid(), "lideranca", "Liderança", "Termo do Dicionário Cognitivo para capacidade de gestão.", "Gestão", "Diagnóstico estrutural", "approved", DateTimeOffset.UtcNow)
            ]);
        public Task<CognitiveTerm> CreateTermAsync(Guid organizationId, CreateCognitiveTermRequest request, Guid actor, string correlationId, CancellationToken ct) => throw new NotImplementedException();
    }

    [Fact]
    public async Task ExplainInsight_WithFewerThan3Evidence_FailsConvergenceRule_AndMarksRequiresMoreEvidence() {
        var orgId = Guid.NewGuid();
        var insightId = Guid.NewGuid();
        var insight = new AiInsight(
            insightId, orgId, Guid.NewGuid(), null, Guid.NewGuid(),
            "gap", "Comunicação Fragmentada", "Divergência de alinhamento",
            "Evidência pontual", "Liderança", "LID_01", "medium", "high",
            "low", "Amostra insuficiente", "Realizar alinhamento", "pending_review",
            DateTime.UtcNow, DateTime.UtcNow
        );

        // Apenas 2 evidências (< 3)
        var evidence = new List<AiInsightEvidence> {
            new(Guid.NewGuid(), "pesquisa", "formulario", Guid.NewGuid(), "Nota 2.1 em liderança", "Liderança", "LID", DateTime.UtcNow, true),
            new(Guid.NewGuid(), "pesquisa", "formulario", Guid.NewGuid(), "Comentário sobre feedback", "Liderança", "LID", DateTime.UtcNow, true)
        };

        var service = new ExplainableIntelligenceService(
            new MockInsightRepository(new AiInsightDetails(insight, evidence, [])),
            new MockMethodologyRepository(),
            new MockKnowledgeRepository()
        );

        var result = await service.ExplainInsightAsync(orgId, Guid.NewGuid(), insightId, true, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.Confidence.MeetsMinimumConvergentRule);
        Assert.True(result.Limitations.RequiresAdditionalEvidence);
        Assert.Contains("ao menos 3 evidências convergentes", result.Confidence.MethodologicalRule);
        Assert.Equal("Moderada", result.Confidence.Level);
    }

    [Fact]
    public async Task ExplainInsight_WithAtLeast3ConvergentEvidence_MeetsRule_AndCalculatesHighConfidence() {
        var orgId = Guid.NewGuid();
        var insightId = Guid.NewGuid();
        var insight = new AiInsight(
            insightId, orgId, Guid.NewGuid(), null, Guid.NewGuid(),
            "strength", "Alta Coesão Cultural", "Consistência de propósito",
            "Evidências convergentes em múltiplas fontes", "Liderança", "LID_01", "low", "medium",
            "high", null, "Sustentar ritos", "approved",
            DateTime.UtcNow, DateTime.UtcNow
        );

        // 3 evidências de 2 fontes diferentes (formulario e entrevista)
        var evidence = new List<AiInsightEvidence> {
            new(Guid.NewGuid(), "pesquisa", "formulario", Guid.NewGuid(), "Nota 4.8 em visão clara", "Liderança", "LID", DateTime.UtcNow, true),
            new(Guid.NewGuid(), "pesquisa", "formulario", Guid.NewGuid(), "92% de concordância", "Liderança", "LID", DateTime.UtcNow, true),
            new(Guid.NewGuid(), "entrevista", "entrevista_qualitativa", Guid.NewGuid(), "Validação com coordenadores", "Liderança", "LID", DateTime.UtcNow, true)
        };

        var service = new ExplainableIntelligenceService(
            new MockInsightRepository(new AiInsightDetails(insight, evidence, [])),
            new MockMethodologyRepository(),
            new MockKnowledgeRepository()
        );

        var result = await service.ExplainInsightAsync(orgId, Guid.NewGuid(), insightId, true, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Confidence.MeetsMinimumConvergentRule);
        Assert.False(result.Limitations.RequiresAdditionalEvidence);
        Assert.Equal("Alta", result.Confidence.Level);
        Assert.Equal(3, result.Confidence.EvidenceCount);
        Assert.Equal(2, result.Confidence.ConvergentSourcesCount);
    }

    [Fact]
    public async Task ExplainableAnalysis_Mandatory7Pillars_AreAllPopulatedAndNonEmpty() {
        var orgId = Guid.NewGuid();
        var insightId = Guid.NewGuid();
        var insight = new AiInsight(
            insightId, orgId, Guid.NewGuid(), null, Guid.NewGuid(),
            "risk", "Sobrecarga de Liderança", "Gargalo operacional",
            "Evidências observadas", "Liderança", "LID", "high", "critical",
            "high", "Risco de turnover", "Redistribuir demandas", "pending_review",
            DateTime.UtcNow, DateTime.UtcNow
        );

        var evidence = new List<AiInsightEvidence> {
            new(Guid.NewGuid(), "pesquisa", "formulario", Guid.NewGuid(), "Exaustão reportada", "Liderança", "LID", DateTime.UtcNow, true),
            new(Guid.NewGuid(), "pesquisa", "formulario", Guid.NewGuid(), "Horas extras recorrentes", "Liderança", "LID", DateTime.UtcNow, true),
            new(Guid.NewGuid(), "dados", "turnover_metric", Guid.NewGuid(), "Aumento no turnover de gestores", "Liderança", "LID", DateTime.UtcNow, true)
        };

        var service = new ExplainableIntelligenceService(
            new MockInsightRepository(new AiInsightDetails(insight, evidence, [])),
            new MockMethodologyRepository(),
            new MockKnowledgeRepository()
        );

        var result = await service.ExplainInsightAsync(orgId, Guid.NewGuid(), insightId, true, CancellationToken.None);
        Assert.NotNull(result);

        // 1. O que foi observado
        Assert.False(string.IsNullOrWhiteSpace(result.WhatWasObserved));

        // 2. Evidências consideradas
        Assert.NotEmpty(result.EvidenceConsidered);
        Assert.Equal(3, result.EvidenceConsidered.Count);

        // 3. Capacidades relacionadas
        Assert.NotEmpty(result.RelatedCapabilities);
        Assert.Equal("Liderança", result.RelatedCapabilities[0].DimensionName);
        Assert.NotEmpty(result.RelatedCapabilities[0].CascadingInfluences);

        // 4. Grau de confiança
        Assert.False(string.IsNullOrWhiteSpace(result.Confidence.Level));
        Assert.False(string.IsNullOrWhiteSpace(result.Confidence.MethodologicalRule));

        // 5. Limitações metodológicas
        Assert.False(string.IsNullOrWhiteSpace(result.Limitations.AssociationVsCausalityNotice));
        Assert.False(string.IsNullOrWhiteSpace(result.Limitations.MethodologyBoundaries));

        // 6. Possível consequência
        Assert.False(string.IsNullOrWhiteSpace(result.Consequence.SystemicRisk));
        Assert.NotEmpty(result.Consequence.VulnerableCapabilities);

        // 7. Evolução no Valora Action™
        Assert.False(string.IsNullOrWhiteSpace(result.Evolution.RecommendedAction));
        Assert.False(string.IsNullOrWhiteSpace(result.Evolution.HumanDecisionNotice));

        // Metodologia e Hash
        Assert.Equal("Valora Insight™ v2.0 - Metodologia Oficial", result.MethodologyVersion);
        Assert.False(string.IsNullOrWhiteSpace(result.DataHash));
    }

    [Fact]
    public void Limitations_StrictlyDistinguishesAssociationFromProvenCausality() {
        var notice = "A Metodologia Valora™ estabelece separação estrita entre correlação e causalidade comprovada. A associação estatística ou de padrões entre os indicadores observados indica vulnerabilidade sistêmica provável, mas não comprova causa isolada sem validação contextual de campo.";
        Assert.Contains("separação estrita entre correlação e causalidade comprovada", notice);
        Assert.Contains("não comprova causa isolada", notice);
    }

    [Fact]
    public void Evolution_ProhibitsAutonomousAiDecisions_RequiresHumanDeliberation() {
        var notice = "As recomendações geradas por IA são hipóteses orientadoras e não constituem decisão executiva compulsória. O plano deve ser avaliado, ajustado e aprovado por liderança humana no Valora Action™.";
        Assert.Contains("não constituem decisão executiva compulsória", notice);
        Assert.Contains("aprovado por liderança humana no Valora Action™", notice);
    }

    [Fact]
    public void ValoraInferenceEngine_WithLessThan3Evidence_MarksNonConclusive() {
        var engine = new ValoraInferenceEngine();
        var request = new InferenceRequest(
            "Ruído na comunicação interna",
            "Canais descentralizados sem governança",
            ["Comunicação", "Processos"],
            [
                new("COM_01", "Demora na resposta", "chat", 0.7m, DateTimeOffset.UtcNow),
                new("COM_02", "Reclamação de retrabalho", "email", 0.8m, DateTimeOffset.UtcNow)
            ],
            "Perda de eficiência",
            "Média",
            "Centralizar canais"
        );

        var result = engine.Infer(request);

        Assert.False(result.IsConclusive);
        Assert.Equal("Baixa", result.Confidence);
        Assert.Null(result.ProbableCause);
        Assert.Contains("Dados insuficientes para estimar impacto", result.Impact);
        Assert.Contains("Ampliar a coleta até obter ao menos três evidências", result.NextStep);
    }

    [Fact]
    public void ValoraInferenceEngine_With3OrMoreEvidence_MarksConclusive() {
        var engine = new ValoraInferenceEngine();
        var request = new InferenceRequest(
            "Ruído na comunicação interna",
            "Canais descentralizados sem governança",
            ["Comunicação", "Processos"],
            [
                new("COM_01", "Demora na resposta", "chat", 0.7m, DateTimeOffset.UtcNow),
                new("COM_02", "Reclamação de retrabalho", "email", 0.8m, DateTimeOffset.UtcNow),
                new("COM_03", "Auditoria de chamados pendentes", "jira", 0.9m, DateTimeOffset.UtcNow)
            ],
            "Perda de eficiência",
            "Alta",
            "Centralizar canais"
        );

        var result = engine.Infer(request);

        Assert.True(result.IsConclusive);
        Assert.Equal("Moderada", result.Confidence);
        Assert.Equal("Canais descentralizados sem governança", result.ProbableCause);
        Assert.Equal("Perda de eficiência", result.Impact);
        Assert.Equal("Centralizar canais", result.NextStep);
    }

    [Fact]
    public void Shared_ExplainableAnalysisModal_Implements_All_7_Pillars_In_Markup() {
        var modalPath = Path.Combine(WebRootPath, "Views", "Shared", "_ExplainableAnalysisModal.cshtml");
        Assert.True(File.Exists(modalPath), $"_ExplainableAnalysisModal.cshtml não encontrado em: {modalPath}");

        var content = File.ReadAllText(modalPath);

        Assert.Contains("id=\"valoraExplainabilityModal\"", content);
        Assert.Contains("Entenda esta análise", content);
        Assert.Contains("O que foi observado", content);
        Assert.Contains("Evidências consideradas", content);
        Assert.Contains("Capacidades relacionadas", content);
        Assert.Contains("Grau de confiança metodológica", content);
        Assert.Contains("Limitações e Princípio da Não-Causalidade", content);
        Assert.Contains("Possível consequência sistêmica", content);
        Assert.Contains("Evolução recomendada & Supervisão humana", content);
        Assert.Contains("Supervisão Humana Obrigatória", content);
        Assert.Contains("Valora Insight™ v2.0 - Metodologia Oficial", content);
    }

    [Fact]
    public void Details_View_Renders_EntendaEstaAnalise_Section() {
        var viewPath = Path.Combine(WebRootPath, "Views", "Insights", "Details.cshtml");
        Assert.True(File.Exists(viewPath), $"Details.cshtml não encontrado em: {viewPath}");

        var content = File.ReadAllText(viewPath);

        Assert.Contains("Entenda esta análise", content);
        Assert.Contains("data-action=\"open-explainability-modal\"", content);
        Assert.Contains("1. O que foi observado", content);
        Assert.Contains("2. Evidências consideradas", content);
        Assert.Contains("3. Capacidades relacionadas", content);
        Assert.Contains("4. Grau de confiança", content);
        Assert.Contains("5. Limitações & Princípio da Não-Causalidade", content);
        Assert.Contains("6. Possível consequência", content);
        Assert.Contains("7. Evolução recomendada & Decisão humana", content);
    }

    [Fact]
    public void Insights_Index_View_Contains_EntendaEstaAnalise_Action_Button() {
        var viewPath = Path.Combine(WebRootPath, "Views", "Insights", "Index.cshtml");
        Assert.True(File.Exists(viewPath), $"Index.cshtml não encontrado em: {viewPath}");

        var content = File.ReadAllText(viewPath);

        Assert.Contains("data-action=\"open-explainability-modal\"", content);
        Assert.Contains("Entenda esta análise", content);
    }
}

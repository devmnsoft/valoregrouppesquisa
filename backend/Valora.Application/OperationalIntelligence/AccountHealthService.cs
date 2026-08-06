namespace Valora.Application.OperationalIntelligence;

public enum AccountHealthStatus { Critical, Attention, Good, Excellent }
public sealed record AccountHealthInput(bool OnboardingCompleted, bool HasMainUnit, int DepartmentCount, int InvitedMemberCount, int PublishedSurveyCount, int ResponseCount, bool PlanWithinLimits, int GeneratedReportCount, int PendingRecommendationCount, int ActiveActionCount);
public sealed record AccountHealthResult(int Score, AccountHealthStatus Status, string Explanation, IReadOnlyList<OperationalNextAction> NextActions);
public sealed record OperationalNextAction(string Code, string Title, string Description, string Route, string Priority, string Icon);

/// <summary>Regra determinística de prontidão usada na central da empresa e no dashboard.</summary>
public sealed class AccountHealthService
{
    public AccountHealthResult Evaluate(AccountHealthInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var checks = new[] { input.OnboardingCompleted, input.HasMainUnit, input.DepartmentCount > 0, input.InvitedMemberCount > 0, input.PublishedSurveyCount > 0, input.ResponseCount > 0, input.PlanWithinLimits, input.GeneratedReportCount > 0, input.PendingRecommendationCount == 0, input.ActiveActionCount > 0 };
        var score = checks.Count(value => value) * 10;
        var status = score switch { < 40 => AccountHealthStatus.Critical, < 70 => AccountHealthStatus.Attention, < 90 => AccountHealthStatus.Good, _ => AccountHealthStatus.Excellent };
        var explanation = status switch
        {
            AccountHealthStatus.Critical => "Sua conta precisa de configurações essenciais antes de gerar indicadores confiáveis.",
            AccountHealthStatus.Attention => "Sua conta está quase pronta. Conclua os próximos passos para iniciar a inteligência operacional.",
            AccountHealthStatus.Good => "Sua operação está saudável; avance no acompanhamento e nos planos de ação.",
            _ => "Excelente: sua empresa mantém uma rotina completa de diagnóstico e melhoria contínua."
        };
        return new(score, status, explanation, BuildActions(input));
    }

    private static IReadOnlyList<OperationalNextAction> BuildActions(AccountHealthInput value)
    {
        var actions = new List<OperationalNextAction>();
        if (!value.OnboardingCompleted) actions.Add(new("company", "Concluir cadastro da empresa", "Revise os dados institucionais e finalize o onboarding.", "/Organization#org-profile", "Crítica", "building"));
        if (!value.HasMainUnit) actions.Add(new("unit", "Criar primeira unidade", "Defina a matriz para organizar setores e gestores.", "/Organization#org-structure", "Alta", "building"));
        if (value.DepartmentCount == 0) actions.Add(new("department", "Cadastrar setores", "Crie ao menos um setor para segmentar as análises.", "/Organization#org-structure", "Alta", "layers"));
        if (value.InvitedMemberCount == 0) actions.Add(new("members", "Convidar gestores", "Distribua responsabilidades com escopos seguros.", "/Users", "Média", "users"));
        if (value.PublishedSurveyCount == 0) actions.Add(new("survey", "Publicar primeira pesquisa", "Inicie o primeiro ciclo de escuta da empresa.", "/Surveys", "Alta", "file-question"));
        else if (value.ResponseCount > 0) actions.Add(new("results", "Analisar respostas recebidas", "Identifique tendências e dimensões prioritárias.", "/Results", "Alta", "chart-radar"));
        if (value.ResponseCount > 0 && value.GeneratedReportCount == 0) actions.Add(new("report", "Gerar relatório executivo", "Compartilhe a leitura consolidada com a liderança.", "/Reports", "Média", "file-text"));
        if (value.PendingRecommendationCount > 0 && value.ActiveActionCount == 0) actions.Add(new("action", "Criar plano de ação", "Transforme recomendações prioritárias em execução.", "/ActionPlans", "Alta", "activity"));
        if (!value.PlanWithinLimits) actions.Add(new("plan", "Revisar plano e limites", "Libere capacidade para manter sua operação ativa.", "/Plans", "Crítica", "sparkles"));
        return actions.Take(4).ToArray();
    }
}

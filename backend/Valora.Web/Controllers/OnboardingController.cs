using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common;
using Valora.Application.Contracts;

namespace Valora.Web.Controllers;

[Authorize]
[Route("Onboarding")]
public sealed class OnboardingController(
    ILogger<OnboardingController> logger,
    IDbConnectionFactory connections,
    ICurrentOrganizationProvider orgProvider) : Controller {
    private static readonly OnboardingItemViewModel[] Items =
    [
        new("organization", "Selecionar organização", "Defina o contexto seguro de trabalho.", "/Organization"),
        new("plan", "Configurar assinatura e plano", "Confirme capacidades e limites contratados.", "/Plans"),
        new("branding", "Configurar branding", "Aplique identidade e informações institucionais.", "/Organization"),
        new("users", "Criar usuários", "Cadastre responsáveis com o menor privilégio.", "/Users"),
        new("form", "Criar formulário", "Estruture dimensões e perguntas em rascunho.", "/Forms/Create"),
        new("publish", "Publicar formulário", "Revise e confirme a versão que será preservada.", "/Forms"),
        new("diagnostic", "Criar diagnóstico", "Use um formulário publicado e defina o público.", "/Diagnostics/New"),
        new("invites", "Enviar convites", "Revise destinatários, canal e privacidade.", "/Surveys/PublicLinks"),
        new("responses", "Acompanhar respostas", "Monitore adesão e suficiência da amostra.", "/Responses"),
        new("result", "Gerar resultado", "Calcule e valide evidências antes de compartilhar.", "/Results"),
        new("report", "Gerar relatório", "Emita um entregável para o público autorizado.", "/Reports"),
        new("action", "Criar plano de ação", "Defina ação, responsável, prioridade e prazo.", "/ActionCenter/Plans/Create")
    ];

    [HttpGet("")]
    [HttpGet("Checklist")]
    public async Task<IActionResult> Index() => View("Checklist", await BuildChecklistAsync());

    [HttpGet("Organization")]
    public IActionResult Organization() => View("Step", Step("organization", "Configuração da organização", "/Organization"));

    [HttpGet("FirstForm")]
    public IActionResult FirstForm() => View("Step", Step("form", "Primeiro formulário", "/Forms/Create"));

    [HttpGet("FirstDiagnostic")]
    public IActionResult FirstDiagnostic() => View("Step", Step("diagnostic", "Primeiro diagnóstico", "/Diagnostics/New"));

    [HttpPost("Progress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Progress(string code, bool completed) {
        if (!Items.Any(item => item.Code.Equals(code, StringComparison.OrdinalIgnoreCase))) {
            logger.LogWarning("Onboarding item invalid. Code={Code} CorrelationId={CorrelationId}", code, HttpContext.TraceIdentifier);
            TempData["Error"] = "Não foi possível atualizar essa etapa. Atualize a página e tente novamente.";
            return RedirectToAction(nameof(Index));
        }

        // Persist to database if organization is resolved
        var orgContext = orgProvider.GetCurrent();
        if (orgContext.OrganizationId is { } orgId && orgId != Guid.Empty) {
            try {
                using var conn = connections.Create();
                var stepStatus = completed ? "completed" : "in_progress";

                // Ensure onboarding_states is updated
                await conn.ExecuteAsync("""
                    INSERT INTO valorapesquisa.onboarding_states (organization_id, status, current_step, updated_at)
                    VALUES (@orgId, 'in_progress', @code, now())
                    ON CONFLICT (organization_id) DO UPDATE SET
                        current_step = EXCLUDED.current_step,
                        updated_at = now();
                    """, new { orgId, code });

                // Ensure checklist exists
                var checklistId = await conn.ExecuteScalarAsync<Guid?>("""
                    INSERT INTO valorapesquisa.onboarding_checklists (organization_id, status, created_at, updated_at)
                    VALUES (@orgId, 'in_progress', now(), now())
                    ON CONFLICT DO NOTHING
                    RETURNING id;
                    """, new { orgId });

                if (checklistId is null || checklistId == Guid.Empty) {
                    checklistId = await conn.ExecuteScalarAsync<Guid>("""
                        SELECT id FROM valorapesquisa.onboarding_checklists
                        WHERE organization_id = @orgId AND deleted_at IS NULL
                        ORDER BY created_at DESC LIMIT 1;
                        """, new { orgId });
                }

                if (checklistId != Guid.Empty) {
                    await conn.ExecuteAsync("""
                        INSERT INTO valorapesquisa.onboarding_steps (checklist_id, organization_id, step_code, status, completed_at, updated_at)
                        VALUES (@checklistId, @orgId, @code, @stepStatus, CASE WHEN @completed THEN now() ELSE NULL END, now())
                        ON CONFLICT (checklist_id, step_code) DO UPDATE SET
                            status = EXCLUDED.status,
                            completed_at = EXCLUDED.completed_at,
                            updated_at = now();
                        """, new { checklistId, orgId, code, stepStatus, completed });
                }
            }
            catch (Exception ex) {
                logger.LogError(ex, "Failed to persist onboarding progress to database. OrgId={OrgId} Code={Code} CorrelationId={CorrelationId}",
                    orgId, code, HttpContext.TraceIdentifier);
            }
        }

        // Update cookie fallback
        Response.Cookies.Append($"Valora.Onboarding.{code}", completed ? "1" : "0", new CookieOptions {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            MaxAge = TimeSpan.FromDays(180),
            IsEssential = true
        });

        logger.LogInformation("Onboarding progress updated. Code={Code} Completed={Completed} UserId={UserId} CorrelationId={CorrelationId}",
            code, completed, User.FindFirst("sub")?.Value, HttpContext.TraceIdentifier);
        TempData["Success"] = completed ? "Etapa concluída. Seu próximo passo já está indicado." : "Etapa reaberta para revisão.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<OnboardingChecklistViewModel> BuildChecklistAsync() {
        var orgContext = orgProvider.GetCurrent();
        var dbSteps = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        if (orgContext.OrganizationId is { } orgId && orgId != Guid.Empty) {
            try {
                using var conn = connections.Create();
                var rows = await conn.QueryAsync<(string StepCode, string Status)>(
                    """
                    SELECT s.step_code, s.status
                    FROM valorapesquisa.onboarding_steps s
                    WHERE s.organization_id = @orgId
                    """, new { orgId });
                foreach (var r in rows) {
                    dbSteps[r.StepCode] = string.Equals(r.Status, "completed", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Could not query onboarding steps from DB, falling back to cookies. OrgId={OrgId}", orgId);
            }
        }

        var states = Items.Select(item => {
            var completed = dbSteps.TryGetValue(item.Code, out var dbVal)
                ? dbVal
                : Request.Cookies[$"Valora.Onboarding.{item.Code}"] == "1";
            return item with { Completed = completed };
        }).ToArray();

        var done = states.Count(item => item.Completed);
        return new(states, done, states.Length, states.FirstOrDefault(item => !item.Completed));
    }

    private static OnboardingStepViewModel Step(string code, string title, string actionUrl) {
        var item = Items.Single(value => value.Code == code);
        return new(code, title, item.Description, "Abrir configuração", actionUrl);
    }
}

public sealed record OnboardingItemViewModel(string Code, string Title, string Description, string Url, bool Completed = false);
public sealed record OnboardingChecklistViewModel(IReadOnlyCollection<OnboardingItemViewModel> Items, int Completed, int Total, OnboardingItemViewModel? Next);
public sealed record OnboardingStepViewModel(string Code, string Title, string Description, string NextLabel, string NextUrl);

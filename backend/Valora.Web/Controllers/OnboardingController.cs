using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
[Route("Onboarding")]
public sealed class OnboardingController(ILogger<OnboardingController> logger) : Controller
{
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
    public IActionResult Index() => View("Checklist", BuildChecklist());

    [HttpGet("Organization")]
    public IActionResult Organization() => View("Step", Step("organization", "Configuração da organização", "/Organization"));

    [HttpGet("FirstForm")]
    public IActionResult FirstForm() => View("Step", Step("form", "Primeiro formulário", "/Forms/Create"));

    [HttpGet("FirstDiagnostic")]
    public IActionResult FirstDiagnostic() => View("Step", Step("diagnostic", "Primeiro diagnóstico", "/Diagnostics/New"));

    [HttpPost("Progress")]
    [ValidateAntiForgeryToken]
    public IActionResult Progress(string code, bool completed)
    {
        if (!Items.Any(item => item.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning("Onboarding item invalid. Code={Code} CorrelationId={CorrelationId}", code, HttpContext.TraceIdentifier);
            TempData["Error"] = "Não foi possível atualizar essa etapa. Atualize a página e tente novamente.";
            return RedirectToAction(nameof(Index));
        }

        Response.Cookies.Append($"Valora.Onboarding.{code}", completed ? "1" : "0", new CookieOptions
        {
            HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = Request.IsHttps,
            MaxAge = TimeSpan.FromDays(180), IsEssential = true
        });
        logger.LogInformation("Onboarding progress updated. Code={Code} Completed={Completed} UserId={UserId} CorrelationId={CorrelationId}",
            code, completed, User.FindFirst("sub")?.Value, HttpContext.TraceIdentifier);
        TempData["Success"] = completed ? "Etapa concluída. Seu próximo passo já está indicado." : "Etapa reaberta para revisão.";
        return RedirectToAction(nameof(Index));
    }

    private OnboardingChecklistViewModel BuildChecklist()
    {
        var states = Items.Select(item => item with { Completed = Request.Cookies[$"Valora.Onboarding.{item.Code}"] == "1" }).ToArray();
        var done = states.Count(item => item.Completed);
        return new(states, done, states.Length, states.FirstOrDefault(item => !item.Completed));
    }

    private static OnboardingStepViewModel Step(string code, string title, string actionUrl)
    {
        var item = Items.Single(value => value.Code == code);
        return new(code, title, item.Description, "Abrir configuração", actionUrl);
    }
}

public sealed record OnboardingItemViewModel(string Code, string Title, string Description, string Url, bool Completed = false);
public sealed record OnboardingChecklistViewModel(IReadOnlyCollection<OnboardingItemViewModel> Items, int Completed, int Total, OnboardingItemViewModel? Next);
public sealed record OnboardingStepViewModel(string Code, string Title, string Description, string NextLabel, string NextUrl);

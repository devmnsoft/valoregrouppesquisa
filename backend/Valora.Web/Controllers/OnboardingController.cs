using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
[Route("Onboarding")]
public sealed class OnboardingController : Controller
{
    private static readonly string[] Allowed = ["Organization", "Structure", "Users", "FirstDiagnostic", "Review"];

    [HttpGet("")]
    public IActionResult Index() => View("Step", Step("Organization"));

    [HttpGet("{step}")]
    public IActionResult StepPage(string step)
    {
        var canonical = Allowed.FirstOrDefault(value => value.Equals(step, StringComparison.OrdinalIgnoreCase));
        return canonical is null ? NotFound() : View("Step", Step(canonical));
    }

    private static OnboardingStepViewModel Step(string code)
    {
        var position = Array.IndexOf(Allowed, code) + 1;
        var copy = code switch
        {
            "Organization" => ("Identidade da organização", "Confirme os dados que contextualizam todas as leituras de maturidade.", "Estrutura"),
            "Structure" => ("Estrutura e lideranças", "Organize unidades, áreas e equipes para produzir análises responsáveis por contexto.", "Usuários"),
            "Users" => ("Pessoas e acessos", "Defina responsáveis e conceda somente os acessos necessários para cada jornada.", "Primeiro diagnóstico"),
            "FirstDiagnostic" => ("Primeiro diagnóstico", "Escolha o template, a privacidade e o público da primeira coleta.", "Revisão"),
            _ => ("Revisão para ativação", "Revise a configuração e inicie a evolução com governança e rastreabilidade.", "Concluir onboarding")
        };
        return new(code, copy.Item1, copy.Item2, copy.Item3, position, Allowed.Length,
            position < Allowed.Length ? $"/Onboarding/{Allowed[position]}" : "/Diagnostics/New");
    }
}

public sealed record OnboardingStepViewModel(string Code, string Title, string Description, string NextLabel,
    int Position, int Total, string NextUrl);

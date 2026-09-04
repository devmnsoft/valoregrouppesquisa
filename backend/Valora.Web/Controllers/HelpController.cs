using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Web.Controllers;

[Authorize]
[Route("Help")]
public sealed class HelpController : Controller
{
    private static readonly IReadOnlyDictionary<string, HelpTopicViewModel> Topics =
        new Dictionary<string, HelpTopicViewModel>(StringComparer.OrdinalIgnoreCase)
        {
            ["GettingStarted"] = Topic("GettingStarted", "Primeiros passos", "Ative o contexto correto antes de começar.",
                ("Selecione a organização", "Confirme no topo qual organização receberá os dados.", "/Organization"),
                ("Conclua a configuração", "Revise plano, marca e pessoas no checklist guiado.", "/Onboarding/Checklist"),
                ("Crie o primeiro diagnóstico", "Publique um formulário e defina público e período.", "/Onboarding/FirstDiagnostic")),
            ["Modules"] = Topic("Modules", "Módulos", "Entenda a jornada operacional de ponta a ponta.",
                ("Formulários", "Estruture perguntas, faça preview e publique uma versão imutável.", "/Forms"),
                ("Diagnósticos", "Escolha um formulário publicado, convide e acompanhe respostas.", "/Diagnostics"),
                ("Resultados e ações", "Valide evidências, gere a leitura e transforme achados em ações.", "/Results")),
            ["Forms"] = Topic("Forms", "Formulários", "Crie instrumentos claros, válidos e rastreáveis.",
                ("Criar", "Informe nome, objetivo, dimensões e perguntas com ajuda de campo.", "/Forms/Create"),
                ("Revisar", "Use o preview e corrija a versão em rascunho.", "/Forms"),
                ("Publicar", "Confirme a publicação. Depois dela, mudanças estruturais exigem nova versão.", "/Forms")),
            ["Diagnostics"] = Topic("Diagnostics", "Diagnósticos e respostas", "Conduza coletas com contexto e privacidade.",
                ("Configurar", "Defina formulário publicado, público, prazo e regra de anonimato.", "/Diagnostics/New"),
                ("Convidar", "Revise destinatários e canal antes de enviar.", "/Surveys/PublicLinks"),
                ("Acompanhar", "Monitore adesão sem expor respostas individuais.", "/Responses")),
            ["Reports"] = Topic("Reports", "Resultados, relatórios e certificados", "Emita entregáveis somente após validação.",
                ("Gerar resultado", "Calcule quando a coleta tiver respostas suficientes e valide as evidências.", "/Results"),
                ("Emitir relatório", "Escolha o resultado válido, período e público autorizado.", "/Reports"),
                ("Emitir certificado", "Confirme a elegibilidade, emita e gerencie o link público.", "/Certificates")),
            ["Admin"] = Topic("Admin", "Administração", "Gerencie a plataforma com menor privilégio e auditoria.",
                ("Organizações e plano", "Super Admin deve selecionar uma organização antes de operar seu contexto.", "/AdminHub/Organizations"),
                ("Usuários e perfis", "Conceda apenas os acessos necessários e revise sessões.", "/AdminHub/Users"),
                ("Auditoria", "Consulte alterações críticas por pessoa, organização e correlação.", "/AdminHub/Audit"))
        };

    [HttpGet("")]
    public IActionResult Index() => View(new HelpCenterViewModel(Topics.Values.ToArray()));

    [HttpGet("{topic}")]
    public IActionResult TopicPage(string topic) => Topics.TryGetValue(topic, out var model) ? View("Topic", model) : NotFound();

    private static HelpTopicViewModel Topic(string code, string title, string summary,
        params (string Title, string Description, string Url)[] steps) => new(code, title, summary, steps);
}

public sealed record HelpCenterViewModel(IReadOnlyCollection<HelpTopicViewModel> Topics);
public sealed record HelpTopicViewModel(string Code, string Title, string Summary,
    IReadOnlyCollection<(string Title, string Description, string Url)> Steps);

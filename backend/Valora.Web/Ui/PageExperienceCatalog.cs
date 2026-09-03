using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Valora.Web.Models;

namespace Valora.Web.Ui;

public sealed class PageExperienceCatalog
{
    private static readonly IReadOnlyDictionary<string, string[]> Guidance = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Dashboard"] = ["acompanhar a situação da organização e priorizar o que requer atenção", "na abertura da rotina de gestão", "revise alertas e abra o indicador ou diagnóstico que precisa de ação", "interprete tendências no contexto; um sinal isolado não determina uma decisão"],
        ["Forms"] = ["criar, revisar e versionar formulários de diagnóstico", "antes de iniciar uma nova coleta", "revise perguntas e regras antes de publicar", "formulários publicados não devem sofrer alteração estrutural; crie uma nova versão"],
        ["Diagnostics"] = ["acompanhar diagnósticos ativos e identificar pendências de resposta", "durante o planejamento e a execução das coletas", "selecione um diagnóstico para conferir público, prazo e progresso", "não conclua análises sem respostas e evidências suficientes"],
        ["Surveys"] = ["organizar pesquisas e acompanhar a coleta de respostas", "ao preparar ou monitorar uma pesquisa", "revise o formulário e os respondentes antes de publicar", "proteja dados pessoais e respeite a finalidade informada aos participantes"],
        ["Results"] = ["interpretar resultados consolidados e evidências registradas", "depois que a coleta possuir dados suficientes", "valide os achados com as pessoas responsáveis antes de agir", "baixa confiança exige revisão humana; associação não comprova causalidade"],
        ["Reports"] = ["preparar entregáveis rastreáveis para públicos autorizados", "depois da revisão humana dos resultados", "confira período, evidências e destinatários antes de gerar", "não compartilhe informações sensíveis sem autorização"],
        ["Certificates"] = ["emitir e consultar certificados autorizados", "após a conclusão válida do diagnóstico", "confira titular e escopo antes da emissão", "emissão e download ficam registrados para auditoria"],
        ["ActionCenter"] = ["transformar achados validados em planos e ações acompanháveis", "quando houver responsável, prazo e evidência de origem", "priorize uma ação e defina seu responsável", "só conclua ações com evidência verificável"],
        ["Evolution"] = ["acompanhar ciclos de evolução e comparar progresso ao longo do tempo", "nas revisões periódicas da organização", "abra o ciclo vigente e registre a próxima revisão", "compare períodos equivalentes e preserve o contexto das mudanças"],
        ["Journey"] = ["consultar a linha do tempo de eventos relevantes da organização", "em revisões, auditorias e acompanhamento de marcos", "filtre o período ou abra um evento para verificar sua origem", "eventos apoiam a análise, mas não substituem a decisão humana"],
        ["Governance"] = ["organizar ciclos, reuniões, decisões e responsáveis de governança", "antes e depois de fóruns decisórios", "registre pauta, decisão, responsável e evidência", "aprove apenas conteúdo revisado pelas pessoas autorizadas"],
        ["Indicators"] = ["monitorar indicadores, metas, medições e alertas", "nas rotinas de acompanhamento de desempenho", "revise alertas e registre a medição mais recente", "confirme fonte, período e unidade antes de comparar valores"],
        ["Administration"] = ["administrar configurações e acessos da organização", "quando houver mudança operacional autorizada", "selecione o módulo que precisa ser configurado", "aplique o menor privilégio e confirme ações críticas"],
        ["Saas"] = ["administrar clientes, planos e operação global da plataforma", "somente em atividades de administração SaaS", "selecione uma organização antes de alterar sua configuração", "ações globais exigem perfil Super Admin e ficam auditadas"],
        ["Onboarding"] = ["conduzir a implantação com etapas, responsáveis e evidências", "durante a ativação de uma organização", "continue pela próxima etapa pendente", "marque uma etapa como concluída somente após validar sua evidência"],
        ["SuccessCenter"] = ["acompanhar adoção, saúde e próximos passos do cliente", "nas rotinas de sucesso do cliente", "revise sinais de risco e combine uma ação com o cliente", "sinais automáticos exigem contexto e validação humana"],
        ["Plans"] = ["comparar recursos e escolher o plano adequado à operação", "quando os limites atuais não atenderem à necessidade", "compare limites e solicite a alteração do plano", "nenhum upgrade é aplicado sem confirmação"],
        ["Benchmarks"] = ["comparar resultados com coortes elegíveis sem expor organizações", "quando houver amostra suficiente para uma comparação responsável", "defina os filtros e confira a composição da amostra", "amostras abaixo do mínimo de privacidade não são exibidas"],
        ["Methodology"] = ["administrar conceitos, escalas e vínculos que sustentam as análises", "ao revisar ou evoluir a metodologia institucional", "valide o impacto e documente a justificativa antes de publicar", "mudanças metodológicas devem preservar a rastreabilidade histórica"],
        ["AdminHub"] = ["gerenciar organizações, usuários, papéis e configurações da plataforma", "em atividades administrativas autorizadas", "escolha o cadastro ou controle que precisa de revisão", "privilégios elevados e alterações críticas devem seguir o menor acesso necessário"],
        ["Organization"] = ["manter a identidade, a marca e os dados operacionais da organização", "quando houver uma alteração institucional validada", "revise os dados e salve somente as mudanças confirmadas", "a organização ativa define o contexto das informações e permissões exibidas"],
        ["Architecture"] = ["organizar a estrutura de unidades, áreas, papéis e relações da organização", "ao implantar ou atualizar a arquitetura organizacional", "selecione um elemento da estrutura e revise suas dependências", "alterações estruturais podem afetar públicos, indicadores e comparações"],
        ["DecisionCenter"] = ["reunir alertas e decisões que exigem acompanhamento executivo", "durante a priorização e a revisão gerencial", "abra o sinal mais relevante e registre a decisão responsável", "recomendações apoiam a análise, mas a decisão permanece humana"],
        ["Communications"] = ["preparar comunicações e acompanhar seus envios", "ao convidar participantes ou informar responsáveis", "revise destinatários, conteúdo e canal antes de enviar", "dados pessoais e mensagens devem respeitar finalidade, consentimento e acesso"],
        ["Settings"] = ["configurar preferências operacionais da experiência Valora", "quando uma regra ou preferência autorizada precisar mudar", "revise o impacto da configuração antes de salvar", "configurações podem depender do plano, perfil e organização selecionada"],
        ["Account"] = ["acessar sua conta Valora Insight™ com segurança", "para iniciar ou recuperar uma sessão", "informe suas credenciais ou use a recuperação de acesso", "não compartilhe sua senha; confirme o endereço antes de entrar"]
    };

    public PageExperienceViewModel Create(string controller, ITempDataDictionary tempData)
    {
        var copy = Guidance.TryGetValue(controller, out var found) ? found :
            ["realizar as atividades desta área com contexto e rastreabilidade", "quando esta etapa fizer parte da sua rotina", "revise as informações disponíveis e escolha a próxima ação", "confirme dados, permissões e evidências antes de concluir"];
        return new($"Use esta página para {copy[0]}.", $"Quando usar: {copy[1]}.",
            $"Próximo passo recomendado: {copy[2]}.", $"Cuidado: {copy[3]}.",
            "Algumas ações podem ficar indisponíveis conforme seu perfil, plano ou organização selecionada.",
            Read(tempData, "Success", "SuccessMessage"), Read(tempData, "Error", "ErrorMessage"), Read(tempData, "Warning", "WarningMessage"),
            Read(tempData, "Info", "Information", "InformationMessage"));
    }

    private static string? Read(ITempDataDictionary data, params string[] keys)
    {
        foreach (var key in keys)
            if (data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value?.ToString())) return value!.ToString();
        return null;
    }
}

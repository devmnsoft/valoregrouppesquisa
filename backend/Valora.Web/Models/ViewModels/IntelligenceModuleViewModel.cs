namespace Valora.Web.Models.ViewModels;

public sealed record IntelligenceModuleViewModel(
    string Slug, string ProductName, string Eyebrow, string Title, string Description,
    string DataSource, string ApiResource, string RequiredPlan, string EmptyMessage,
    IReadOnlyList<string> Capabilities)
{
    private static readonly IReadOnlyDictionary<string, IntelligenceModuleViewModel> Catalog =
        new[]
        {
            Module("methodology", "Núcleo Metodológico Valora™", "Base conceitual oficial", "Regras, maturidade e terminologia em uma fonte única", "Fundação versionada consultada pelos módulos de inteligência, evolução, entregáveis e governança.", "concepts", "Professional", "Conceitos oficiais", "Regras de evidência", "Níveis de maturidade"),
            Module("dictionary", "Dicionário Cognitivo Valora™", "Núcleo metodológico", "A linguagem oficial da evolução organizacional", "Conceitos, terminologia, evidências observáveis, relações e diretrizes versionadas da Metodologia Valora™.", "concepts", "Professional", "Busca conceitual", "Evidências de maturidade", "Terminologia oficial"),
            Module("cognitive-map", "Mapa Cognitivo Valora™", "Relações sistêmicas", "Influências e efeitos cascata", "Mapa consultável das relações entre capacidades, sem interpretar um indicador isoladamente.", "cognitive-map", "Professional", "Matriz de influência", "Causalidade metodológica", "Efeito cascata"),
            Module("inference", "Motor de Inferência Valora™", "Inferência rastreável", "Conclusões somente com evidências convergentes", "Cada conclusão exige ao menos três evidências, distingue sintoma de causa e informa confiança, impacto, prioridade e próximo passo.", "runs", "Professional", "Regra de três evidências", "Grau de confiança", "Hash e versão metodológica"),
            Module("dashboard", "Valora Dashboard™", "Visão executiva", "Decisões começam por uma leitura confiável", "Síntese da maturidade, confiança, gaps e volume de evidências da organização.", "dashboard", "Insight", "Índices consolidados", "Alertas de evidência", "Prioridades do ciclo"),
            Module("metrics", "Valora Metrics™", "Indicadores", "Métricas com contexto, não números isolados", "Indicadores oficiais com definição, fonte e rastreabilidade para evitar conclusões sem evidência.", "indicators", "Insight", "Catálogo de indicadores", "Fonte e peso", "Leitura por capacidade"),
            Module("indices", "Índices Valora™", "Maturidade em contexto", "Doze índices oficiais em escala de 0 a 100", "IMO™, ICS™, IIO™, IGO™, ICO™, ILI™, IPO™, IDO™, IAC™, IAR™, IIS™ e ISO™ combinam consistência, recorrência, relações e evolução; não uma média isolada.", "dashboard", "Professional", "Classificação de maturidade", "Fatores contribuintes", "Histórico e tendência"),
            Module("radar", "Valora Radar™", "Leitura multidimensional", "Equilíbrio organizacional em perspectiva", "Visualização das dimensões avaliadas no mesmo ciclo, sem transformar benchmark em ranking público.", "heatmap", "Professional", "Radar por dimensão", "Gap estrutural", "Evidências agregadas"),
            Module("heatmap", "Valora Heatmap™", "Mapa de atenção", "Onde aprofundar a investigação", "Mapa de capacidades baseado apenas em resultados consolidados e com alerta de insuficiência.", "heatmap", "Professional", "Faixas de atenção", "Volume de evidências", "Prioridade para validação"),
            Module("benchmark", "Valora Benchmark™", "Referência contextual", "Comparar para compreender, nunca para ranquear", "Comparação privada entre ciclos e recortes autorizados. Não há ranking público de organizações.", "runs", "Professional", "Comparação entre ciclos", "Contexto da amostra", "Privacidade por padrão"),
            Module("insights", "Valora Insights IA™", "Motor de inferência", "Da observação ao plano de evolução", "Conclusões rastreáveis com observação, evidências, correlação, causa provável, impacto, prioridade e confiança.", "runs", "Professional", "Hipóteses explícitas", "Confiança informada", "Regra de três evidências"),
            Module("action", "Valora Action™", "Execução", "Evidências transformadas em compromisso", "Planos vinculados a justificativa, responsável, indicador e critério objetivo de conclusão.", "action-plans", "Professional", "Prioridade e complexidade", "Responsabilidade", "Histórico auditável"),
            Module("evolution", "Valora Evolution™", "Evolução longitudinal", "Aprender entre ciclos", "Série histórica com variação observada e projeção somente quando houver histórico suficiente.", "evolution", "Professional", "Baseline", "Variação entre ciclos", "Projeção identificada"),
            Module("journey", "Valora Journey™", "Jornada organizacional", "A memória da evolução", "Linha do tempo de diagnósticos, decisões, ações e marcos relevantes da organização.", "journey", "Professional", "Marcos do ciclo", "Decisões registradas", "Ações concluídas"),
            Module("executive-report", "Valora Executive Report™", "Governança executiva", "Uma narrativa verificável para a liderança", "Estrutura de devolutiva executiva baseada nos resultados disponíveis e nos limites da evidência.", "dashboard", "Professional", "Síntese executiva", "Riscos e prioridades", "Exportação preparada"),
            Module("one-on-one", "Valora One-on-One™", "Desenvolvimento responsável", "Conversas estruturadas sem expor respostas individuais", "Estrutura preparada para pautas e compromissos, preservando privacidade e finalidade organizacional.", "journey", "Enterprise", "Pauta estruturada", "Compromissos", "Privacidade individual"),
            Module("power-bi", "Power BI™", "Integrações", "Dados preparados para análise autorizada", "Camada de exportação preparada para datasets agregados; conexão externa não é simulada nesta fase.", "indicators", "Enterprise", "Dataset agregado", "Dicionário de métricas", "Pronto para integração"),
            Module("platform-governance", "Governança da Plataforma", "Controle e rastreabilidade", "Governança técnica distinta da capacidade organizacional", "Trilhas de acesso, configurações, indicadores, diagnósticos, exportações e ciclos com escopo organizacional.", "dashboard", "Enterprise", "Trilha auditável", "Alterações sensíveis", "Governança de acesso")
        }.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);

    public static IntelligenceModuleViewModel? Find(string slug) => Catalog.GetValueOrDefault(slug);

    private static IntelligenceModuleViewModel Module(string slug, string name, string eyebrow, string title,
        string description, string apiResource, string plan, params string[] capabilities) =>
        new(slug, name, eyebrow, title, description, "Dados organizacionais agregados", apiResource, plan,
            "Ainda não há dados suficientes para apresentar esta leitura com segurança.", capabilities);
}

namespace Valora.Application.Experience;

public sealed record OfficialTemplate(string Code, string Name, string Description, string RecommendedPlan, int Questions,
    IReadOnlyList<string> Dimensions, bool Certificate, bool Report, bool Comparison, int EstimatedMinutes);

public static class OfficialTemplateCatalog
{
    public static IReadOnlyList<OfficialTemplate> All { get; } =
    [
        Template("diagnostico-essencial", "Diagnóstico Essencial", "Uma leitura objetiva para iniciar a jornada de maturidade.", "Essencial", 18, 8, "Estratégia", "Pessoas", "Processos"),
        Template("cultura", "Cultura Organizacional", "Entenda comportamentos, valores e coerência cultural.", "Profissional", 24, 10, "Valores", "Comunicação", "Engajamento"),
        Template("governanca", "Governança", "Avalie decisões, controles, responsabilidades e transparência.", "Executivo", 28, 12, "Direcionamento", "Riscos", "Controles"),
        Template("lideranca", "Liderança", "Revele a capacidade de direcionar, desenvolver e inspirar times.", "Profissional", 22, 9, "Direção", "Desenvolvimento", "Confiança"),
        Template("clima", "Clima Interno", "Monitore segurança, pertencimento e experiência das pessoas.", "Profissional", 26, 10, "Bem-estar", "Pertencimento", "Reconhecimento"),
        Template("setor", "Maturidade por Setor", "Compare áreas com critérios consistentes e acionáveis.", "Executivo", 30, 12, "Gestão", "Execução", "Resultados"),
        Template("unidade", "Maturidade por Unidade", "Identifique boas práticas e unidades que precisam de apoio.", "Executivo", 30, 12, "Gestão", "Pessoas", "Performance"),
        Template("franquias", "Rede de Franquias", "Padronização e performance para redes distribuídas.", "Enterprise", 36, 15, "Marca", "Operação", "Governança"),
        Template("holding", "Holding ou Grupo Empresarial", "Visão consolidada de governança e evolução do grupo.", "Enterprise", 40, 18, "Estratégia", "Sinergia", "Governança"),
        new("personalizada", "Pesquisa Personalizada", "Comece com uma estrutura livre e adapte cada dimensão.", "Todos", 0, ["Definidas por você"], false, true, false, 5)
    ];

    public static OfficialTemplate? Find(string code) => All.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    private static OfficialTemplate Template(string code, string name, string description, string plan, int questions, int minutes, params string[] dimensions) =>
        new(code, name, description, plan, questions, dimensions, true, true, true, minutes);
}

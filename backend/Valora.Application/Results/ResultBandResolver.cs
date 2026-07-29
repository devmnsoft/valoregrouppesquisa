namespace Valora.Application.Results;

public sealed class ResultBandResolver
{
    public (string band, string recommendation) Resolve(decimal normalized5) => normalized5 switch
    {
        < 2 => ("Crítico", "Priorizar plano de ação imediato e revisão dos processos essenciais."),
        < 3.5m => ("Em estruturação", "Consolidar governança, indicadores e automação dos pontos mais frágeis."),
        < 4.5m => ("Estruturada", "Aprimorar escala, padronização e acompanhamento por dimensão."),
        _ => ("Alta maturidade", "Sustentar excelência e expandir benchmark e melhoria contínua.")
    };
}

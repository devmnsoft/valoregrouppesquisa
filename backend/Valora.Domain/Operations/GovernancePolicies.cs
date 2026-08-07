namespace Valora.Domain.Operations;

public static class GovernanceCatalog
{
    public static readonly string[] ImplementationSteps =
    [
        "Cadastro da empresa", "Validação de CNPJ e dados", "Configuração de plano",
        "Cadastro de unidade principal", "Cadastro de setores", "Convite dos gestores",
        "Configuração de identidade visual", "Escolha do template de pesquisa",
        "Publicação da primeira pesquisa", "Validação do link público", "Teste de resultado",
        "Teste de relatório", "Teste de certificado", "Treinamento do cliente", "Go-live"
    ];

    public static readonly string[] ProductionChecklist =
    [
        "Empresa cadastrada", "Plano ativo", "Assinatura configurada", "Unidade principal criada",
        "Setores cadastrados", "Usuários administradores convidados", "Permissões revisadas",
        "Pesquisa de teste publicada", "Resposta de teste realizada", "Resultado gerado",
        "Relatório testado", "Certificado testado", "LGPD configurada", "Identidade visual revisada",
        "Canais de suporte configurados", "Backup ativo", "Auditoria ativa", "Logs sem erro crítico",
        "Mobile validado", "E-mail validado", "WhatsApp validado"
    ];
}

public static class AnonymityPolicy
{
    public const string InsufficientDataMessage = "Não há respostas suficientes para exibir este recorte sem comprometer o anonimato.";
    public static bool CanExposeSegment(bool anonymous, int responseCount, int minimumResponses = 5) =>
        !anonymous || responseCount >= Math.Max(3, minimumResponses);
    public static bool CanExposeIndividual(bool anonymous) => !anonymous;
}

public static class BackupFreshnessPolicy
{
    public static string Resolve(DateTime? lastSuccessfulAt, int maximumAgeHours, bool lastAttemptFailed, DateTime utcNow)
    {
        if (maximumAgeHours <= 0) return "not_configured";
        if (lastAttemptFailed) return "failed";
        if (lastSuccessfulAt is null) return "not_configured";
        return utcNow - lastSuccessfulAt.Value > TimeSpan.FromHours(maximumAgeHours) ? "delayed" : "current";
    }
}

public static class MigrationSafetyPolicy
{
    public static IReadOnlyList<string> Validate(string type, IReadOnlyDictionary<string, string> values)
    {
        var errors = new List<string>();
        if (!values.TryGetValue("nome", out var name) || string.IsNullOrWhiteSpace(name)) errors.Add("Nome é obrigatório.");
        if (values.TryGetValue("email", out var email) && email.Length > 0 && (!email.Contains('@') || email.Length > 254)) errors.Add("E-mail inválido.");
        if (type == "companies" && (!values.TryGetValue("cnpj", out var cnpj) || cnpj.Count(char.IsDigit) != 14)) errors.Add("CNPJ deve conter 14 dígitos.");
        if (type == "responses" && !values.ContainsKey("pesquisa")) errors.Add("Pesquisa é obrigatória para respostas.");
        return errors;
    }
}

namespace Valora.Api.Operations;

public interface IConfigurationValidationService
{
    ConfigurationValidationResult Validate();
}

public sealed record ConfigurationValidationIssue(
    string Code,
    string Category,
    string Severity,
    string Message,
    string RecommendedAction,
    bool IsBlocking);

public sealed record ConfigurationValidationResult(
    string OverallStatus,
    string EnvironmentName,
    bool IsProduction,
    IReadOnlyList<ConfigurationValidationIssue> Issues,
    int Warnings,
    int CriticalErrors,
    DateTimeOffset CheckedAt);

public sealed class ConfigurationValidationService(IConfiguration configuration, IWebHostEnvironment environment)
    : IConfigurationValidationService
{
    private const string RequiredMessage = "Configuração obrigatória ausente para este recurso. Verifique o painel de Saúde do Sistema.";

    public ConfigurationValidationResult Validate()
    {
        var issues = new List<ConfigurationValidationIssue>();
        Require(issues, "DB_CONNECTION", "database", ConnectionString(), true, "Configure ConnectionStrings__Postgres.");
        Require(issues, "JWT_ISSUER", "authentication", configuration["Jwt:Issuer"], true, "Configure Jwt__Issuer.");
        Require(issues, "JWT_AUDIENCE", "authentication", configuration["Jwt:Audience"], true, "Configure Jwt__Audience.");
        var signingKey = configuration["Jwt:SigningKey"] ?? configuration["Jwt:Secret"];
        Require(issues, "JWT_SIGNING_KEY", "authentication", signingKey, true, "Use uma chave aleatória com pelo menos 32 caracteres.");
        if (environment.IsProduction() && !string.IsNullOrWhiteSpace(signingKey) && signingKey.Length < 32)
            Add(issues, "JWT_WEAK_KEY", "authentication", "critical", "A chave JWT não atende ao mínimo de segurança de produção.", "Use uma chave aleatória com pelo menos 32 caracteres.", true);

        Require(issues, "PUBLIC_BASE_URL", "urls", configuration["App:PublicBaseUrl"], true, "Configure App__PublicBaseUrl com HTTPS.");
        Require(issues, "ADMIN_BASE_URL", "urls", configuration["App:AdminBaseUrl"], true, "Configure App__AdminBaseUrl com HTTPS.");
        Require(issues, "STORAGE_PROVIDER", "storage", configuration["Storage:Provider"], false, "Configure Storage__Provider.");
        Require(issues, "STORAGE_BASE_PATH", "storage", configuration["Storage:BasePath"], false, "Configure Storage__BasePath fora da pasta publicada.");

        if (configuration.GetValue<bool>("Email:Enabled"))
        {
            Require(issues, "SMTP_HOST", "email", configuration["Email:SmtpHost"] ?? configuration["Email:Smtp:Host"], false, "Configure o host SMTP.");
            Require(issues, "EMAIL_FROM", "email", configuration["Email:From"] ?? configuration["Email:FromEmail"], false, "Configure o remetente.");
        }
        if (environment.IsProduction() && configuration.GetValue<bool>("App:EnableDemoSeed", configuration.GetValue<bool>("Demo:SeedEnabled")))
            Add(issues, "DEMO_SEED_PRODUCTION", "seed", "critical", "Seed de demonstração não pode operar em produção.", "Defina App__EnableDemoSeed=false.", true);
        if (environment.IsProduction() && configuration.GetValue<bool>("App:EnableDetailedErrors"))
            Add(issues, "DETAILED_ERRORS_PRODUCTION", "environment", "critical", "Erros detalhados não podem ser exibidos em produção.", "Defina App__EnableDetailedErrors=false.", true);
        if (environment.IsProduction() && !configuration.GetValue<bool>("Security:RequireHttps", true))
            Add(issues, "HTTPS_DISABLED", "http_security", "critical", "HTTPS é obrigatório em produção.", "Defina Security__RequireHttps=true.", true);
        if (environment.IsProduction() && string.IsNullOrWhiteSpace(configuration["Backup:LastKnownAt"]))
            Add(issues, "BACKUP_NOT_RECORDED", "backup", "critical", "Nenhum backup conhecido foi registrado para produção.", "Execute o runbook de backup e registre o evento operacional.", true);

        var critical = issues.Count(x => x.Severity == "critical");
        var warnings = issues.Count(x => x.Severity == "warning");
        return new(critical > 0 ? "critical" : warnings > 0 ? "warning" : "healthy", environment.EnvironmentName,
            environment.IsProduction(), issues, warnings, critical, DateTimeOffset.UtcNow);
    }

    private string? ConnectionString() => configuration.GetConnectionString("Postgres") ?? configuration.GetConnectionString("DefaultConnection");
    private static void Require(List<ConfigurationValidationIssue> issues, string code, string category, string? value, bool blocking, string action)
    {
        if (string.IsNullOrWhiteSpace(value)) Add(issues, code, category, blocking ? "critical" : "warning", RequiredMessage, action, blocking);
    }
    private static void Add(List<ConfigurationValidationIssue> issues, string code, string category, string severity, string message, string action, bool blocking) =>
        issues.Add(new(code, category, severity, message, action, blocking));
}

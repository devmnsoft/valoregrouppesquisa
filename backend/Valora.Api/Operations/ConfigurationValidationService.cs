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
        var signingKey = configuration["Jwt:SigningKey"];
        Require(issues, "JWT_SIGNING_KEY", "authentication", signingKey, true, "Use uma chave aleatória com pelo menos 32 caracteres.");
        if (!string.IsNullOrWhiteSpace(signingKey) && signingKey.Trim().Length < 32)
            Add(issues, "JWT_WEAK_KEY", "authentication", "critical", "A chave JWT não atende ao mínimo de 32 caracteres.", "Configure Jwt__SigningKey com uma chave aleatória de pelo menos 32 caracteres.", true);
        if (environment.IsProduction() && IsKnownPlaceholder(signingKey))
            Add(issues, "JWT_DEMO_KEY_PRODUCTION", "authentication", "critical", "A chave JWT de demonstração não pode ser usada em produção.", "Forneça um segredo exclusivo por Jwt__SigningKey ou secret manager.", true);

        Require(issues, "PUBLIC_BASE_URL", "urls", configuration["App:PublicBaseUrl"], true, "Configure App__PublicBaseUrl com HTTPS.");
        Require(issues, "ADMIN_BASE_URL", "urls", configuration["App:AdminBaseUrl"], true, "Configure App__AdminBaseUrl com HTTPS.");
        Require(issues, "STORAGE_PROVIDER", "storage", configuration["Storage:Provider"], false, "Configure Storage__Provider.");
        Require(issues, "STORAGE_BASE_PATH", "storage", configuration["Storage:BasePath"], false, "Configure Storage__BasePath fora da pasta publicada.");
        if (!configuration.GetValue<bool>("Certificates:PdfEnabled") && !configuration.GetValue<bool>("Reports:PdfEnabled"))
            Add(issues, "PDF_NOT_CONFIGURED", "pdf", "warning", "Geração de PDF não configurada.", "Habilite e configure Certificates__PdfEnabled ou Reports__PdfEnabled quando o recurso for utilizado.", false);

        if (configuration.GetValue<bool>("Email:Enabled"))
        {
            Require(issues, "SMTP_HOST", "email", configuration["Email:SmtpHost"] ?? configuration["Email:Smtp:Host"], false, "Configure o host SMTP.");
            Require(issues, "EMAIL_FROM", "email", configuration["Email:From"] ?? configuration["Email:FromEmail"], false, "Configure o remetente.");
        }
        else
            Add(issues, "EMAIL_NOT_CONFIGURED", "email", "warning", "Envio de e-mail não configurado.", "Configure Email e defina Email__Enabled=true quando o recurso for utilizado.", false);
        if (environment.IsProduction() && configuration.GetValue<bool>("App:EnableDemoSeed", configuration.GetValue<bool>("Demo:SeedEnabled")))
            Add(issues, "DEMO_SEED_PRODUCTION", "seed", "critical", "Seed de demonstração não pode operar em produção.", "Defina App__EnableDemoSeed=false.", true);
        if (environment.IsProduction() && configuration.GetValue<bool>("App:EnableDetailedErrors"))
            Add(issues, "DETAILED_ERRORS_PRODUCTION", "environment", "critical", "Erros detalhados não podem ser exibidos em produção.", "Defina App__EnableDetailedErrors=false.", true);
        if (environment.IsProduction() && !configuration.GetValue<bool>("Security:RequireHttps", true))
            Add(issues, "HTTPS_DISABLED", "http_security", "critical", "HTTPS é obrigatório em produção.", "Defina Security__RequireHttps=true.", true);
        if (environment.IsProduction() && configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() is not { Length: > 0 })
            Add(issues, "CORS_ORIGINS", "http_security", "critical", "As origens CORS permitidas não foram configuradas para produção.", "Configure Cors__AllowedOrigins__0 com a origem HTTPS da aplicação Web.", true);
        if (environment.IsProduction() && string.IsNullOrWhiteSpace(configuration["Backup:LastKnownAt"]))
            Add(issues, "BACKUP_NOT_RECORDED", "backup", "critical", "Nenhum backup conhecido foi registrado para produção.", "Execute o runbook de backup e registre o evento operacional.", false);

        var critical = issues.Count(x => x.Severity == "critical");
        var warnings = issues.Count(x => x.Severity == "warning");
        return new(critical > 0 ? "critical" : warnings > 0 ? "warning" : "healthy", environment.EnvironmentName,
            environment.IsProduction(), issues, warnings, critical, DateTimeOffset.UtcNow);
    }

    private string? ConnectionString() => configuration.GetConnectionString("Postgres") ?? configuration.GetConnectionString("DefaultConnection");
    private static bool IsKnownPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var normalized = value.Trim();
        return normalized.StartsWith("DEV_ONLY_", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("DEMO", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase);
    }

    private static void Require(List<ConfigurationValidationIssue> issues, string code, string category, string? value, bool blocking, string action)
    {
        if (string.IsNullOrWhiteSpace(value)) Add(issues, code, category, blocking ? "critical" : "warning", RequiredMessage, action, blocking);
    }
    private static void Add(List<ConfigurationValidationIssue> issues, string code, string category, string severity, string message, string action, bool blocking) =>
        issues.Add(new(code, category, severity, message, action, blocking));
}

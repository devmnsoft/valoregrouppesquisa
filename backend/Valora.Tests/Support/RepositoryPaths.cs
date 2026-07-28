namespace Valora.Tests.Support;

public static class RepositoryPaths
{
    public static string RepositoryRoot { get; } = ResolveRepositoryRoot();
    public static string BackendRoot => Path.Combine(RepositoryRoot, "backend");
    public static string ApiRoot => Path.Combine(BackendRoot, "Valora.Api");
    public static string ApplicationRoot => Path.Combine(BackendRoot, "Valora.Application");
    public static string DomainRoot => Path.Combine(BackendRoot, "Valora.Domain");
    public static string InfrastructureRoot => Path.Combine(BackendRoot, "Valora.Infrastructure");
    public static string WebRoot => Path.Combine(BackendRoot, "Valora.Web");
    public static string TestsRoot => Path.Combine(BackendRoot, "Valora.Tests");
    public static string CanonicalDatabaseScript => Path.Combine(BackendRoot, "database", "postgresql", "banco_completo.sql");
    public static string MigrationsDirectory => Path.Combine(BackendRoot, "database", "postgresql", "migrations");
    public static string RootPackageJson => Path.Combine(RepositoryRoot, "package.json");

    public static string BackendFile(params string[] segments) => SafeCombine(BackendRoot, segments);
    public static string RepositoryFile(params string[] segments) => SafeCombine(RepositoryRoot, segments);
    public static string MigrationFile(string fileName) => SafeCombine(MigrationsDirectory, [fileName]);

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "Valora.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Nao foi possivel localizar a raiz que contem backend/Valora.sln.");
    }

    private static string SafeCombine(string root, IReadOnlyList<string> segments)
    {
        if (segments.Count == 0 || segments.Any(segment => string.IsNullOrWhiteSpace(segment) || Path.IsPathRooted(segment)))
            throw new ArgumentException("Informe somente segmentos de caminho relativos.", nameof(segments));

        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(segments.Aggregate(root, Path.Combine));
        if (!candidate.StartsWith(fullRoot, StringComparison.Ordinal))
            throw new ArgumentException("O caminho solicitado escapa da raiz permitida.", nameof(segments));
        return candidate;
    }
}

namespace Valora.Tests.Support;

public static class RepositoryPaths
{
    public static string RepositoryRoot { get; } = FindRepositoryRoot();
    public static string BackendRoot => Path.Combine(RepositoryRoot, "backend");
    public static string CanonicalDatabaseScript => Path.Combine(BackendRoot, "database", "postgresql", "banco_completo.sql");
    public static string MigrationsDirectory => Path.Combine(BackendRoot, "database", "postgresql", "migrations");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "Valora.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Nao foi possivel localizar a raiz que contem backend/Valora.sln.");
    }
}

using System.Text.RegularExpressions;
using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "Architecture")]
public sealed class ArchitectureFoundationTests
{
    [Fact]
    public void Application_WhenInspectingProjectReferences_DoesNotDependOnInfrastructureOrDapper()
    {
        var project = File.ReadAllText(RepositoryPaths.ApplicationFile("Valora.Application.csproj"));
        Assert.DoesNotContain("Valora.Infrastructure", project, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dapper", project, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Domain_WhenInspectingProjectReferences_DoesNotDependOnOuterLayers()
    {
        var project = File.ReadAllText(RepositoryPaths.DomainFile("Valora.Domain.csproj"));
        foreach (var forbidden in new[] { "Valora.Application", "Valora.Infrastructure", "Valora.Api", "Valora.Web", "Dapper" })
            Assert.DoesNotContain(forbidden, project, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Controllers_WhenInspectingSource_DoNotInstantiateRepositoriesOrDatabaseConnections()
    {
        foreach (var file in Directory.EnumerateFiles(RepositoryPaths.ApiFile("Controllers"), "*.cs"))
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotMatch(@"new\s+\w*Repository\s*\(", source);
            Assert.DoesNotContain("new NpgsqlConnection", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void OfficialProjectsTargetNet10()
    {
        foreach (var project in Directory.EnumerateFiles(RepositoryPaths.BackendRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(project);
            Assert.Contains("<TargetFramework>net10.0</TargetFramework>", text);
            Assert.DoesNotContain("net8.0", text);
        }
    }

    [Fact]
    public void WebProjectDoesNotReferenceDatabasePackages()
    {
        var webProject = File.ReadAllText(RepositoryPaths.WebFile("Valora.Web.csproj"));
        Assert.DoesNotContain("Npgsql", webProject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dapper", webProject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BackendProjectsDoNotDependOnFirebaseRuntime()
    {
        var productionRoots = new[] { RepositoryPaths.ApiRoot, RepositoryPaths.ApplicationRoot, RepositoryPaths.DomainRoot, RepositoryPaths.InfrastructureRoot, RepositoryPaths.WebRoot };
        var runtimeExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".cs", ".csproj", ".js", ".json" };
        var officialFiles = productionRoots.SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && runtimeExtensions.Contains(Path.GetExtension(path)));
        foreach (var file in officialFiles)
        {
            Assert.DoesNotContain("Firebase", File.ReadAllText(file), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PublicCSharpFilesContainSinglePrimaryPublicType()
    {
        var productionRoots = new[] { RepositoryPaths.ApiRoot, RepositoryPaths.ApplicationRoot, RepositoryPaths.DomainRoot, RepositoryPaths.InfrastructureRoot, RepositoryPaths.WebRoot };
        var files = productionRoots.SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
        var publicTypePattern = new Regex(@"^public\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*\b(class|record|interface|enum)\b", RegexOptions.Multiline);
        foreach (var file in files)
        {
            Assert.True(publicTypePattern.Matches(File.ReadAllText(file)).Count <= 1, $"Multiple public primary types in {file}");
        }
    }
}

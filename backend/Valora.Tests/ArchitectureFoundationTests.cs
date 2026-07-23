using System.Text.RegularExpressions;

namespace Valora.Tests;

public sealed class ArchitectureFoundationTests
{
    private static readonly string Root = LocateRepositoryRoot();

    [Fact]
    public void OfficialProjectsTargetNet10()
    {
        foreach (var project in Directory.EnumerateFiles(Path.Combine(Root, "backend"), "*.csproj", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(project);
            Assert.Contains("<TargetFramework>net10.0</TargetFramework>", text);
            Assert.DoesNotContain("net8.0", text);
        }
    }

    [Fact]
    public void WebProjectDoesNotReferenceDatabasePackages()
    {
        var webProject = File.ReadAllText(Path.Combine(Root, "backend", "Valora.Web", "Valora.Web.csproj"));
        Assert.DoesNotContain("Npgsql", webProject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dapper", webProject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BackendOfficialDoesNotReferenceFirebase()
    {
        var officialFiles = Directory.EnumerateFiles(Path.Combine(Root, "backend"), "*.*", SearchOption.AllDirectories)
            .Where(path => !path.Contains("bin") && !path.Contains("obj") && !path.Contains("Valora.Tests"));
        foreach (var file in officialFiles)
        {
            Assert.DoesNotContain("Firebase", File.ReadAllText(file), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PublicCSharpFilesContainSinglePrimaryPublicType()
    {
        var files = Directory.EnumerateFiles(Path.Combine(Root, "backend"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("bin") && !path.Contains("obj"));
        var publicTypePattern = new Regex(@"^public\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*\b(class|record|interface|enum)\b", RegexOptions.Multiline);
        foreach (var file in files)
        {
            Assert.True(publicTypePattern.Matches(File.ReadAllText(file)).Count <= 1, $"Multiple public primary types in {file}");
        }
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "package.json")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}

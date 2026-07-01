using System.Text.RegularExpressions;
using Xunit;

namespace Valora.Tests;

public sealed class OfficialBackendConsolidationTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    private static string Read(string relative) => File.ReadAllText(Path.Combine(Root, relative));
    private static IEnumerable<string> Files(string relative, string pattern) => Directory.Exists(Path.Combine(Root, relative))
        ? Directory.EnumerateFiles(Path.Combine(Root, relative), pattern, SearchOption.AllDirectories)
        : Enumerable.Empty<string>();

    [Fact]
    public void OfficialSolutionIsBackendValoraSln()
    {
        Assert.True(File.Exists(Path.Combine(Root, "backend/Valora.sln")));
        Assert.True(File.Exists(Path.Combine(Root, "BACKEND_OFICIAL_MIGRATION_GUIDE.md")));
        Assert.Contains("backend/Valora.sln", Read("BACKEND_OFICIAL_MIGRATION_GUIDE.md"));
    }

    [Fact]
    public void PackageHasOfficialValidatorAndDoesNotMoveOfficialBuildToBackendV2()
    {
        var packageJson = Read("package.json");
        Assert.Contains("backend:official-validate", packageJson);
        Assert.Contains("tools/validate-backend-official-migration.js", packageJson);
        Assert.DoesNotContain("backend:build\": \"dotnet build backend-v2", packageJson);
        Assert.DoesNotContain("backend:test\": \"dotnet test backend-v2", packageJson);
    }

    [Fact]
    public void ApiAndWebDoNotExposeSensitiveHashNamesInContractsOrUi()
    {
        var files = Files("backend/Valora.Api", "*.cs")
            .Concat(Files("backend/Valora.Application/DTOs", "*.cs"))
            .Concat(Files("backend/Valora.Web", "*.cshtml"))
            .Concat(Files("backend/Valora.Web/wwwroot", "*.js"));
        var joined = string.Join('\n', files.Select(File.ReadAllText));
        Assert.DoesNotMatch(new Regex("public\\s+[^;]*(password_hash|token_hash|result_token_hash)", RegexOptions.IgnoreCase), joined);
        Assert.DoesNotContain("WEB_ADMIN_FAKE", joined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RequiredOfficialDomainEntitiesExist()
    {
        foreach (var entity in new[] { "OrganizationSettings", "OrganizationBranding", "UserProfile", "Role", "Permission", "RolePermission", "Module", "OrganizationModule", "SurveyInvite", "SurveyParticipant", "Response" })
        {
            Assert.True(File.Exists(Path.Combine(Root, $"backend/Valora.Domain/Entities/{entity}.cs")), $"Missing {entity}");
        }
    }
}

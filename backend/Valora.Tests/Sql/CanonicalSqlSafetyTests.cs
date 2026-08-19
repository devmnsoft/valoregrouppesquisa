using System.Text.RegularExpressions;
using Valora.Tests.Support;

namespace Valora.Tests.Sql;

[Trait("Category", "SqlStatic")]
public sealed class CanonicalSqlSafetyTests
{
    private static string Script => File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);

    [Fact]
    public void CanonicalScript_WhenAnalyzed_DoesNotContainDestructiveTableOperations()
    {
        var executable = RemoveComments(Script);
        Assert.DoesNotMatch(new Regex(@"\bDROP\s+TABLE\b", RegexOptions.IgnoreCase), executable);
        Assert.DoesNotMatch(new Regex(@"\bTRUNCATE(?:\s+TABLE)?\b", RegexOptions.IgnoreCase), executable);
    }

    [Fact]
    public void CanonicalScript_WhenDefiningSecurityContracts_RequiresHashesAndNotificationMessage()
    {
        Assert.Matches(@"api_keys\s+ADD\s+COLUMN\s+IF\s+NOT\s+EXISTS\s+key_hash", Script);
        Assert.Matches(@"api_keys\s+ALTER\s+COLUMN\s+key_hash\s+SET\s+NOT\s+NULL", Script);
        Assert.Matches(@"notifications\s+ADD\s+COLUMN\s+IF\s+NOT\s+EXISTS\s+message", Script);
        Assert.Matches(@"notifications\s+ALTER\s+COLUMN\s+message\s+SET\s+NOT\s+NULL", Script);
    }

    [Fact]
    public void CanonicalScript_WhenCreatingTopLevelTables_UsesIdempotentGuard()
    {
        var topLevelCreates = Regex.Matches(RemoveComments(Script), @"(?im)^CREATE\s+TABLE\s+([^;]+)");
        Assert.NotEmpty(topLevelCreates);
        Assert.All(topLevelCreates.Cast<Match>(), match =>
            Assert.StartsWith("CREATE TABLE IF NOT EXISTS", match.Value, StringComparison.OrdinalIgnoreCase));
    }

    private static string RemoveComments(string sql) => Regex.Replace(sql, @"(?m)--.*$", string.Empty);
}

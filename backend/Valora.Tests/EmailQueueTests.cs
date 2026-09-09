using Valora.Tests.Support;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class EmailQueueTests {
    [Fact]
    public void CompleteScriptSeedsEmailQueueAndTemplatesWithoutPassword() {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);
        Assert.Contains("valorapesquisa.email_jobs", sql);
        Assert.Contains("valorapesquisa.email_templates", sql);
        Assert.Contains("recipient_hash", sql);
        Assert.Contains("status", sql);
        Assert.DoesNotContain("SMTP_PASSWORD", sql);
    }
}

using Xunit;
using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class EmailQueueTests
{
    [Fact]
    public void CompleteScriptSeedsEmailQueueAndTemplatesWithoutPassword()
    {
        var sql = File.ReadAllText(RepositoryPaths.CanonicalDatabaseScript);
        Assert.Contains("valorapesquisa.email_jobs", sql);
        Assert.Contains("valorapesquisa.email_templates", sql);
        Assert.Contains("valoragroup@mnsoft.com.br", sql);
        Assert.DoesNotContain("SMTP_PASSWORD", sql);
    }
}

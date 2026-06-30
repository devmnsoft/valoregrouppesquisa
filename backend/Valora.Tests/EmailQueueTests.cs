using Xunit;

namespace Valora.Tests;

public sealed class EmailQueueTests
{
    [Fact]
    public void CompleteScriptSeedsEmailQueueAndTemplatesWithoutPassword()
    {
        var sql = File.ReadAllText(Path.Combine("..", "..", "..", "..", "scriptbd_completo.sql"));
        Assert.Contains("valorapesquisa.email_jobs", sql);
        Assert.Contains("valorapesquisa.email_templates", sql);
        Assert.Contains("valoragroup@mnsoft.com.br", sql);
        Assert.DoesNotContain("SMTP_PASSWORD", sql);
    }
}

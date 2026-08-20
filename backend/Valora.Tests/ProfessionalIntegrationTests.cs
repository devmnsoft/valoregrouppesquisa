using Valora.Application.Integrations;

namespace Valora.Tests;

public sealed class ProfessionalIntegrationTests
{
    [Fact]
    public void Webhook_signature_is_deterministic_and_secret_dependent()
    {
        var first = WebhookSigner.Sign("secret-a", "{\"type\":\"response.received\"}");
        Assert.StartsWith("sha256=", first);
        Assert.Equal(first, WebhookSigner.Sign("secret-a", "{\"type\":\"response.received\"}"));
        Assert.NotEqual(first, WebhookSigner.Sign("secret-b", "{\"type\":\"response.received\"}"));
    }

    [Fact]
    public void Webhook_retry_is_bounded() => Assert.Equal(TimeSpan.FromMinutes(60), WebhookSigner.RetryDelay(20));

    [Fact]
    public void Invalid_import_reports_errors_without_applying_rows()
    {
        var rows = new ExternalImportValidator().ValidateCsv("respondents", "nome;email\nMaria;");
        Assert.Contains(rows.Single().Errors, x => x.Contains("email"));
    }

    [Fact]
    public async Task Disabled_lookup_providers_allow_manual_fallback()
    {
        Assert.False((await new DisabledCnpjLookupService().LookupAsync("123", default)).Available);
        Assert.False((await new DisabledCepLookupService().LookupAsync("123", default)).Available);
    }
}

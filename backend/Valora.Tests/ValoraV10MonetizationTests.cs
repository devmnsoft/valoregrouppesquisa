using Valora.Application.Communication;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.Tests;

public sealed class ValoraV10MonetizationTests
{
    [Fact]
    public async Task Past_due_subscription_is_exposed_as_blocked()
    {
        var repository = new MemorySubscriptionRepository();
        var service = new SubscriptionService(repository);
        await service.UpdateAsync(repository.OrganizationId, Request("past_due"));
        var subscription = await service.GetAsync(repository.OrganizationId);
        Assert.True(subscription!.AccessBlocked);
    }

    [Fact]
    public async Task Manual_payment_requires_positive_amount_and_is_tenant_scoped()
    {
        var repository = new MemorySubscriptionRepository();
        var service = new SubscriptionService(repository);
        await service.UpdateAsync(repository.OrganizationId, Request("active"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterPaymentAsync(repository.OrganizationId, null, new(0, DateTimeOffset.UtcNow, "pix", null, null)));
        var payment = await service.RegisterPaymentAsync(repository.OrganizationId, null, new(250, DateTimeOffset.UtcNow, "pix", "PIX-1", null));
        Assert.Equal(250, payment.Amount);
        Assert.Single(await service.ListPaymentsAsync(repository.OrganizationId));
        Assert.Empty(await service.ListPaymentsAsync(Guid.NewGuid()));
    }

    [Fact]
    public void Whatsapp_builder_uses_official_number_and_never_includes_unsafe_url()
    {
        var link = WhatsAppLinkBuilder.Build("Empresa A", "Ana", "certificado", "reemissão", "javascript:alert(1)", "corr-123");
        Assert.StartsWith("https://wa.me/5591992545353?text=", link);
        Assert.Contains("corr-123", Uri.UnescapeDataString(link));
        Assert.DoesNotContain("javascript", link, StringComparison.OrdinalIgnoreCase);
    }

    private static UpdateSubscriptionRequest Request(string status) => new(Guid.NewGuid(), status, "monthly", 499, 20, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(1), DateTimeOffset.UtcNow.AddDays(10), "Financeiro", "financeiro@example.com", "5591000000000", "pix", null);

    private sealed class MemorySubscriptionRepository : ISubscriptionRepository
    {
        public Guid OrganizationId { get; } = Guid.NewGuid();
        private SubscriptionDto? _value;
        private readonly List<(Guid OrganizationId, ManualPaymentDto Payment)> _payments = [];
        public Task<SubscriptionDto?> GetByOrganizationAsync(Guid organizationId) => Task.FromResult(_value?.OrganizationId == organizationId ? _value : null);
        public Task UpsertAsync(SubscriptionDto subscription) { _value = subscription; return Task.CompletedTask; }
        public Task SetStatusAsync(Guid organizationId, string status) => Task.CompletedTask;
        public Task<ManualPaymentDto> RegisterPaymentAsync(Guid organizationId, Guid? userId, RegisterManualPaymentRequest request)
        {
            var payment = new ManualPaymentDto(Guid.NewGuid(), _value!.Id, request.Amount, request.PaidAt, request.Method, request.Reference, request.Notes, userId, DateTimeOffset.UtcNow);
            _payments.Add((organizationId, payment)); return Task.FromResult(payment);
        }
        public Task<IReadOnlyList<ManualPaymentDto>> ListPaymentsAsync(Guid organizationId) => Task.FromResult<IReadOnlyList<ManualPaymentDto>>(_payments.Where(x => x.OrganizationId == organizationId).Select(x => x.Payment).ToList());
    }
}

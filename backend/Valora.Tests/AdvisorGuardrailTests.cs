using Valora.Application.Advisor;
using Xunit;

namespace Valora.Tests;

public sealed class AdvisorGuardrailTests
{
    [Fact]
    public async Task RejectsWhenAnyRequestedSourceCannotBeResolvedInTenant()
    {
        var repository = new RecordingGuardrailRepository();
        var service = new AdvisorGuardrailService(repository);
        var evidence = new[] { new AdvisorContextOptionDto("indicator", Guid.NewGuid(), "Participação", "Fonte real") };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.Validate(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, evidence, CancellationToken.None));

        Assert.Contains("isolamento", error.Message);
        Assert.Equal("tenant.source_rejected", repository.Rule);
    }

    [Fact]
    public async Task AcceptsOnlyWhenEveryRequestedSourceWasResolved()
    {
        var repository = new RecordingGuardrailRepository();
        var service = new AdvisorGuardrailService(repository);
        var evidence = new[] { new AdvisorContextOptionDto("report", Guid.NewGuid(), "Relatório", "Fonte real") };

        await service.Validate(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, evidence, CancellationToken.None);

        Assert.Null(repository.Rule);
    }

    private sealed class RecordingGuardrailRepository : IAdvisorGuardrailRepository
    {
        public string? Rule { get; private set; }
        public Task Record(Guid organizationId, Guid userId, Guid? conversationId, string rule, string reason, CancellationToken ct)
        {
            Rule = rule;
            return Task.CompletedTask;
        }
    }
}

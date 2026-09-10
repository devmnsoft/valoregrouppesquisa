using Valora.Application.SuccessCenter;

namespace Valora.Tests;

public sealed class SupportCenterContractTests {
    [Fact]
    public async Task Service_rejects_unknown_category_before_persistence() {
        var repository = new RecordingRepository();
        var service = new SupportTicketService(repository);
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() => service.Create(Guid.NewGuid(), Guid.NewGuid(), new CreateTicketCommand { Subject="Assunto válido", Description="Descrição suficientemente detalhada", Category="unknown", Priority="normal" }, default));
        Assert.False(repository.Created);
    }

    [Fact]
    public async Task Empty_reply_is_not_persisted() {
        var repository = new RecordingRepository();
        var service = new SupportTicketService(repository);
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() => service.Reply(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  ", default));
        Assert.False(repository.Replied);
    }

    private sealed class RecordingRepository : ISuccessCenterRepository {
        public bool Created { get; private set; } public bool Replied { get; private set; }
        public Task<Guid> CreateTicket(Guid o, Guid u, CreateTicketCommand c, CancellationToken ct) { Created=true; return Task.FromResult(Guid.NewGuid()); }
        public Task AddTicketMessage(Guid o, Guid u, Guid id, string m, CancellationToken ct) { Replied=true; return Task.CompletedTask; }
        public Task<IReadOnlyList<SupportTicket>> GetTickets(Guid o,CancellationToken ct)=>Task.FromResult<IReadOnlyList<SupportTicket>>([]);
        public Task<SupportTicketDetails?> GetTicket(Guid o,Guid id,bool i,CancellationToken ct)=>Task.FromResult<SupportTicketDetails?>(null);
        public Task SetTicketStatus(Guid o,Guid u,Guid id,string s,CancellationToken ct)=>Task.CompletedTask;
        public Task EnsureOnboarding(Guid o,CancellationToken ct)=>Task.CompletedTask; public Task<IReadOnlyList<OnboardingStep>> GetOnboarding(Guid o,CancellationToken ct)=>Task.FromResult<IReadOnlyList<OnboardingStep>>([]); public Task SetStep(Guid o,Guid u,Guid s,bool c,string? e,CancellationToken ct)=>Task.CompletedTask;
        public Task<UsageEvidence> GetUsageEvidence(Guid o,CancellationToken ct)=>throw new NotImplementedException(); public Task SaveHealth(Guid o,HealthScore s,CancellationToken ct)=>Task.CompletedTask;
        public Task<IReadOnlyList<KnowledgeArticle>> SearchKnowledge(Guid o,string? q,CancellationToken ct)=>Task.FromResult<IReadOnlyList<KnowledgeArticle>>([]); public Task RegisterArticleView(Guid o,Guid u,Guid a,CancellationToken ct)=>Task.CompletedTask;
        public Task<IReadOnlyList<Playbook>> GetPlaybooks(Guid o,CancellationToken ct)=>Task.FromResult<IReadOnlyList<Playbook>>([]); public Task AssignPlaybook(Guid o,Guid u,Guid p,CancellationToken ct)=>Task.CompletedTask; public Task RegisterUsage(Guid o,Guid u,string f,string? c,CancellationToken ct)=>Task.CompletedTask; public Task<IReadOnlyList<FeatureAdoption>> GetAdoption(Guid o,CancellationToken ct)=>Task.FromResult<IReadOnlyList<FeatureAdoption>>([]);
    }
}

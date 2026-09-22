using Valora.Application.Exceptions;
using Valora.Application.ValoraAi;

namespace Valora.Tests;

public sealed class AiInsightReviewConcurrencyTests {
    [Fact]
    public async Task Rejection_requires_a_reason_before_persistence() {
        var repository = new ReviewRepository(AiReviewResult.Applied);
        var service = new AiReviewService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ReviewAsync(
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AiInsightStatuses.Rejected, " ", "cmd-1", 0), default));

        Assert.Null(repository.Command);
    }

    [Fact]
    public async Task Stale_review_is_reported_as_an_explicit_conflict() {
        var repository = new ReviewRepository(AiReviewResult.Conflict);
        var service = new AiReviewService(repository);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.ReviewAsync(
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AiInsightStatuses.Approved, null, "cmd-2", 3), default));
    }

    [Fact]
    public async Task Exact_command_replay_is_successful_and_does_not_require_a_second_decision() {
        var repository = new ReviewRepository(AiReviewResult.Replayed);
        var service = new AiReviewService(repository);

        await service.ReviewAsync(
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AiInsightStatuses.Approved, null, "cmd-3", 0), default);

        Assert.Equal("cmd-3", repository.Command?.CommandKey);
    }

    private sealed class ReviewRepository(AiReviewResult result) : IValoraAiReviewRepository {
        public AiReviewCommand? Command { get; private set; }
        public Task<AiReviewResult> ApplyAsync(AiReviewCommand command, CancellationToken ct) {
            Command = command;
            return Task.FromResult(result);
        }
    }
}

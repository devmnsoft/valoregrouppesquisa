using System.Text.Json;
using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ValoraAi;

namespace Valora.Infrastructure.Repositories;

public sealed class ValoraAiRunRepository(IDbConnectionFactory connections) : IValoraAiRunRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task CreateAsync(ValoraAiRun run, ValoraEvidencePack input, CancellationToken ct)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.valora_ai_runs
              (id, organization_id, diagnosis_id, prompt_code, prompt_version, provider, model, status,
               correlation_id, input_json, run_type, started_at, created_at)
            VALUES (@Id, @OrganizationId, @DiagnosisId, @PromptCode, @PromptVersion, @Provider, @Model, @Status,
                    @CorrelationId, CAST(@InputJson AS jsonb), @PromptCode, @CreatedAt, @CreatedAt)
            """, new { run.Id, run.OrganizationId, run.DiagnosisId, run.PromptCode, run.PromptVersion,
                run.Provider, run.Model, Status = run.Status.ToString(), run.CorrelationId,
                InputJson = JsonSerializer.Serialize(input, JsonOptions), run.CreatedAt }, cancellationToken: ct));
    }

    public async Task CompleteAsync(Guid runId, AiRunStatus status, string? output, ValoraAiValidation? validation,
        ValoraAiProviderResult? usage, string? error, CancellationToken ct)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE valorapesquisa.valora_ai_runs SET status=@Status, output_json=CAST(@Output AS jsonb),
              validation_json=CAST(@Validation AS jsonb), provider=COALESCE(@Provider,provider),
              model=COALESCE(@Model,model), input_tokens=@InputTokens, output_tokens=@OutputTokens,
              estimated_cost=@EstimatedCost, error=@Error, error_message=@Error, completed_at=now(), updated_at=now()
            WHERE id=@RunId
            """, new { RunId = runId, Status = status.ToString(), Output = output,
                Validation = validation is null ? null : JsonSerializer.Serialize(validation, JsonOptions),
                Provider = usage?.Provider, Model = usage?.Model, InputTokens = usage?.InputTokens,
                OutputTokens = usage?.OutputTokens, EstimatedCost = usage?.EstimatedCost, Error = error }, cancellationToken: ct));
    }

    public async Task<AiUsageAllowance> CheckAllowanceAsync(Guid organizationId, CancellationToken ct)
    {
        using var connection = connections.Create();
        var used = await connection.ExecuteScalarAsync<int>(new CommandDefinition("""
            SELECT count(*)::int FROM valorapesquisa.valora_ai_runs
            WHERE organization_id=@OrganizationId AND created_at >= date_trunc('month', now())
            """, new { OrganizationId = organizationId }, cancellationToken: ct));
        const int monthlyLimit = 1000;
        return new AiUsageAllowance(used < monthlyLimit, used, monthlyLimit);
    }

    public async Task RecordReviewAsync(Guid runId, Guid reviewerId, AiRunStatus status, string? note, CancellationToken ct)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.valora_ai_reviews(id, run_id, reviewer_id, status, note)
            VALUES (gen_random_uuid(), @RunId, @ReviewerId, @Status, @Note);
            UPDATE valorapesquisa.valora_ai_runs SET status=@Status, updated_at=now() WHERE id=@RunId
            """, new { RunId = runId, ReviewerId = reviewerId, Status = status.ToString(), Note = note }, cancellationToken: ct));
    }
}

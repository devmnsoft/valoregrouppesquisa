using System.Data;
using Valora.Application.ReadModels;
using Valora.Application.Services;

namespace Valora.Application.Contracts;

public interface IResponseRepository { Task<ResponseReadModel?> GetResultAsync(Guid responseId); Task<Guid> CreateResponseAsync(Guid organizationId, Guid surveyId, Guid formId, Guid formVersionId, string? name, string? email, string? phone, string tokenHash, IDbConnection connection, IDbTransaction transaction); Task AddAnswersAsync(Guid responseId, IEnumerable<ScoredAnswer> answers, IDbConnection connection, IDbTransaction transaction); Task<ResponseReadModel?> GetByIdAsync(Guid responseId); Task<IReadOnlyList<dynamic>> ListAdminAsync(Guid organizationId); Task<AdminResultReadModel?> GetAdminAsync(Guid organizationId, Guid responseId); Task<SubmissionIdempotencyRecord?> GetStoredSubmissionAsync(string key); Task<SubmissionIdempotencyClaim> AcquireSubmissionIdempotencyAsync(string key, Guid organizationId, string requestHash, IDbConnection connection, IDbTransaction transaction); Task StoreSubmissionOutcomeAsync(string key, string responseJson, IDbConnection connection, IDbTransaction transaction); }

/// <summary>Persistent idempotency record for a public survey submission key.</summary>
public sealed record SubmissionIdempotencyRecord(string RequestHash, string? ResponseBody);

/// <summary>Outcome of the idempotency claim: a fresh claim, or the stored sealed response for replay.</summary>
public sealed record SubmissionIdempotencyClaim(bool Claimed, string? StoredResponseBody);

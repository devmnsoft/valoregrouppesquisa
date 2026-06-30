using System.Data;
using Valora.Application.ReadModels;
using Valora.Application.Services;
namespace Valora.Application.Contracts;
public interface IResponseRepository { Task<ResponseReadModel?> GetResultAsync(Guid responseId); Task<Guid> CreateResponseAsync(Guid organizationId,Guid surveyId,Guid formId,string? name,string? email,string? phone,string tokenHash,IDbConnection connection,IDbTransaction transaction); Task AddAnswersAsync(Guid responseId,IEnumerable<ScoredAnswer> answers,IDbConnection connection,IDbTransaction transaction); Task<ResponseReadModel?> GetByIdAsync(Guid responseId); Task<IReadOnlyList<dynamic>> ListAdminAsync(Guid organizationId); Task<dynamic?> GetAdminAsync(Guid organizationId,Guid responseId); }

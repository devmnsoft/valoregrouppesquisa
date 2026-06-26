using System.Data;
namespace Valora.Application.Contracts;
public interface IResponseRepository { Task<dynamic?> GetResultAsync(Guid responseId); Task<Guid> CreateResponseAsync(Guid organizationId,Guid surveyId,Guid formId,string? name,string? email,string? phone,string tokenHash,IDbTransaction transaction); Task AddAnswersAsync(Guid responseId,IEnumerable<dynamic> answers,IDbTransaction transaction); Task<dynamic?> GetByIdAsync(Guid responseId); }

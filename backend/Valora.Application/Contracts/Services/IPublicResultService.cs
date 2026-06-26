using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IPublicResultService { Task<PublicResultResponse> GetAsync(Guid responseId, PublicResultRequest request); }

using ValoraPesquisa.Application.Contracts; using ValoraPesquisa.Application.DTOs;
namespace ValoraPesquisa.Application.Services.Responses;
public sealed class GetPublicResultService(IResponseRepository repo){ public Task<PublicResultDto?> ExecuteAsync(Guid responseId,string token)=>repo.GetResultAsync(responseId,token); }

using ValoraPesquisa.Application.Contracts; using ValoraPesquisa.Application.DTOs; using ValoraPesquisa.Domain.Entities;
namespace ValoraPesquisa.Application.Services.Responses;
public sealed class SubmitPublicSurveyResponseService(IResponseRepository repo){ public Task<SubmitSurveyResponseResult> ExecuteAsync(SurveyResponse response,ResultScore score,string resultToken)=>repo.CreateAsync(response,score,resultToken); }

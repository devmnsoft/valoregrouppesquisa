using ValoraPesquisa.Application.Contracts;
namespace ValoraPesquisa.Application.Services.PublicSurveys;
public sealed class ValidatePublicSurveyService(ISurveyLinkRepository links){ public async Task<bool> ExecuteAsync(Guid surveyId,string org,string token)=>await links.FindValidAsync(surveyId,org,token)!=null; }

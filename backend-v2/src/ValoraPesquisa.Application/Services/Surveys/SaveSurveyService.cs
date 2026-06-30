using ValoraPesquisa.Application.Contracts; using ValoraPesquisa.Application.DTOs; using ValoraPesquisa.Domain.Entities;
namespace ValoraPesquisa.Application.Services.Surveys;
public sealed class SaveSurveyService(ISurveyRepository repo){ public Task<SurveyDto> ExecuteAsync(Survey survey,CurrentUser user)=>repo.SaveAsync(survey,user); }

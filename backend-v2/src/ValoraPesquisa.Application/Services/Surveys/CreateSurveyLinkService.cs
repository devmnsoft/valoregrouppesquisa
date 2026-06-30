using ValoraPesquisa.Application.Contracts; using ValoraPesquisa.Application.DTOs; using ValoraPesquisa.Domain.Entities;
namespace ValoraPesquisa.Application.Services.Surveys;
public sealed class CreateSurveyLinkService(ISurveyLinkRepository repo,ITokenHasher tokens){ public Task<CreatedSurveyLinkDto> ExecuteAsync(Guid surveyId,Guid orgId,string publicUrl,DateTimeOffset? expiresAt){ var token=tokens.NewToken(); return repo.CreateAsync(new SurveyLink(Guid.NewGuid(),surveyId,orgId,tokens.Hash(token),publicUrl,"active",expiresAt,null,DateTimeOffset.UtcNow,null),token); } }

using Valora.Application.Contracts;
using Valora.Application.DTOs;
namespace Valora.Application.Services;
public sealed class PublicSurveyService(PublicSurveyValidator validator,PublicSurveyAssembler assembler,PublicSurveySubmitter submitter):IPublicSurveyService
{
    public async Task<ValidateSurveyResponse> ValidateAsync(Guid surveyId, ValidateSurveyRequest request)
    { var data = await validator.ValidateForReadAsync(surveyId, request); return assembler.Assemble(data.Survey,data.Form,data.Dims,data.Questions,data.Options); }
    public Task<SubmitSurveyResponseResult> SubmitAsync(Guid surveyId, SubmitSurveyResponseRequest request) => submitter.SubmitAsync(surveyId, request);
}

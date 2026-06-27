using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.ReadModels;
namespace Valora.Application.Services;
public sealed class PublicSurveyValidator(ISurveyRepository surveys,IFormRepository forms,IPlanEntitlementService plans)
{
    public async Task<(SurveyPublicReadModel Survey, FormPublicReadModel Form, IReadOnlyList<FormDimensionReadModel> Dims, IReadOnlyList<QuestionPublicReadModel> Questions, IReadOnlyList<QuestionOptionPublicReadModel> Options)> ValidateForReadAsync(Guid surveyId, ValidateSurveyRequest request)
    {
        if (surveyId == Guid.Empty) throw new InvalidOperationException("Pesquisa inválida.");
        if (string.IsNullOrWhiteSpace(request.Token)) throw new UnauthorizedAccessException("Token público obrigatório.");
        var survey = await surveys.GetActivePublicSurveyAsync(surveyId) ?? throw new InvalidOperationException("Pesquisa indisponível.");
        if (!await surveys.ValidatePublicTokenAsync(surveyId, request.Token)) throw new UnauthorizedAccessException("Token público inválido.");
        var form = await forms.GetByIdAsync(survey.FormId) ?? throw new InvalidOperationException("Formulário não encontrado.");
        return (survey, form, await forms.GetDimensionsAsync(survey.FormId), await forms.GetQuestionsAsync(survey.FormId), await forms.GetQuestionOptionsAsync(survey.FormId));
    }
    public async Task ValidateForSubmitAsync(Guid organizationId, SubmitSurveyResponseRequest request, IReadOnlyList<QuestionPublicReadModel> questions)
    {
        if (!request.LgpdConsent) throw new InvalidOperationException("Consentimento LGPD obrigatório.");
        if (questions.Count == 0) throw new InvalidOperationException("Formulário sem perguntas públicas.");
        var limit = await plans.CheckLimitAsync(organizationId, "responsesPerMonth");
        if (!limit.Allowed) throw new InvalidOperationException("Limite do plano atingido para respostas mensais.");
    }
}

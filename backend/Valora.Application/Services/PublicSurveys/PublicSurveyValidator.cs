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
    public async Task ValidateForSubmitAsync(Guid organizationId, SubmitSurveyResponseRequest request, IReadOnlyList<QuestionPublicReadModel> questions, IReadOnlyList<QuestionOptionPublicReadModel> options)
    {
        if (!request.LgpdConsent) throw new InvalidOperationException("Consentimento LGPD obrigatório.");
        if (questions.Count == 0) throw new InvalidOperationException("Formulário sem perguntas públicas.");
        if (request.Answers.GroupBy(answer => answer.QuestionId).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Cada pergunta pode ser respondida apenas uma vez.");
        var knownQuestions = questions.ToDictionary(question => question.Id);
        foreach (var answer in request.Answers)
        {
            if (!knownQuestions.TryGetValue(answer.QuestionId, out var question))
                throw new InvalidOperationException("A resposta contém uma pergunta desconhecida ou de outra versão do formulário.");
            ValidateAnswer(question, answer, options);
        }
        var answered = request.Answers.Select(answer => answer.QuestionId).ToHashSet();
        if (questions.Any(question => question.Required && !answered.Contains(question.Id)))
            throw new InvalidOperationException("Todas as perguntas obrigatórias devem ser respondidas.");
        var limit = await plans.CheckLimitAsync(organizationId, "responsesPerMonth");
        if (!limit.Allowed) throw new InvalidOperationException("Limite do plano atingido para respostas mensais.");
    }

    private static void ValidateAnswer(QuestionPublicReadModel question, PublicSurveyAnswerRequest answer, IReadOnlyList<QuestionOptionPublicReadModel> options)
    {
        var expected = PublicAnswerNormalizer.NormalizeType(question.Type);
        if (PublicAnswerNormalizer.NormalizeType(answer.Type) != expected)
            throw new InvalidOperationException($"O tipo da resposta não corresponde à pergunta: {question.Text}");
        switch (expected)
        {
            case "scale":
            case "likert":
                if (answer.ScaleValue is null || answer.ScaleValue < 1 || answer.ScaleValue > question.MaxScore)
                    throw new InvalidOperationException($"Resposta fora do intervalo da pergunta: {question.Text}");
                break;
            case "number":
                if (answer.NumberValue is null || answer.NumberValue < 0 || answer.NumberValue > question.MaxScore)
                    throw new InvalidOperationException($"Número fora do intervalo da pergunta: {question.Text}");
                break;
            case "single_choice":
                RequireOption(answer.OptionId, question.Id, options);
                break;
            case "multiple_choice":
                var ids = answer.OptionIds ?? [];
                if (ids.Count == 0 || ids.Count != ids.Distinct().Count())
                    throw new InvalidOperationException($"Selecione opções válidas e sem duplicidade: {question.Text}");
                foreach (var id in ids) RequireOption(id, question.Id, options);
                break;
            case "short_text":
                RequireText(answer.TextValue, 500, question.Text);
                break;
            case "long_text":
                RequireText(answer.TextValue, 5_000, question.Text);
                break;
            case "yes_no":
                if (answer.BooleanValue is null) throw new InvalidOperationException($"Informe sim ou não: {question.Text}");
                break;
            default:
                throw new InvalidOperationException($"Tipo de pergunta pública não suportado: {question.Type}");
        }
    }

    private static void RequireOption(Guid? optionId, Guid questionId, IReadOnlyList<QuestionOptionPublicReadModel> options)
    {
        if (optionId is null || !options.Any(option => option.Id == optionId && option.QuestionId == questionId))
            throw new InvalidOperationException("A opção selecionada não pertence à pergunta.");
    }

    private static void RequireText(string? value, int maximumLength, string question)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maximumLength)
            throw new InvalidOperationException($"Revise o limite de texto da pergunta: {question}");
    }
}

using Valora.Application.ReadModels;

namespace Valora.Application.Services;

public sealed record PublicSurveyContext(
    SurveyPublicReadModel Survey,
    FormPublicReadModel Form,
    IReadOnlyList<FormDimensionReadModel> Dimensions,
    IReadOnlyList<QuestionPublicReadModel> Questions,
    IReadOnlyList<QuestionOptionPublicReadModel> Options);

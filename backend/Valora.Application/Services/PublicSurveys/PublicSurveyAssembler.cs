using Valora.Application.DTOs;
using Valora.Application.ReadModels;
namespace Valora.Application.Services;
public sealed class PublicSurveyAssembler
{
    public ValidateSurveyResponse Assemble(SurveyPublicReadModel s,FormPublicReadModel f,IReadOnlyList<FormDimensionReadModel> dims,IReadOnlyList<QuestionPublicReadModel> qs,IReadOnlyList<QuestionOptionPublicReadModel> opts) => new(true, MapSurvey(s), MapForm(f,dims,qs,opts), new PublicCompanyDto(s.OrganizationId,s.OrganizationName,s.PublicName,s.OrganizationSlug), new { required = s.LgpdRequired });
    public PublicSurveyDto MapSurvey(SurveyPublicReadModel s) => new(s.Id,s.Title,s.Description,s.Status,s.LgpdRequired);
    PublicFormDto MapForm(FormPublicReadModel f,IReadOnlyList<FormDimensionReadModel> dims,IReadOnlyList<QuestionPublicReadModel> qs,IReadOnlyList<QuestionOptionPublicReadModel> opts) => new(f.Id,f.Name,f.Description,f.TimeMin,qs.Select(q => new PublicQuestionDto(q.Id,q.Text,q.Type,q.Required,q.MaxScore,dims.FirstOrDefault(d => d.Id == q.DimensionId)?.Name,q.DisplayOrder,opts.Where(o => o.QuestionId == q.Id).Select(o => new PublicQuestionOptionDto(o.Id,o.Text,o.Score,o.DisplayOrder)).ToList())).ToList());
}

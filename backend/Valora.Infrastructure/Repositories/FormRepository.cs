using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ReadModels;
namespace Valora.Infrastructure.Repositories;
public sealed class FormRepository(IDbConnectionFactory f):IFormRepository
{
    public async Task<FormPublicReadModel?> GetAsync(Guid id)=>await GetByIdAsync(id);
    public async Task<FormPublicReadModel?> GetByIdAsync(Guid id){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync<FormPublicReadModel>("SELECT id,name,description,time_min AS TimeMin FROM valorapesquisa.forms WHERE id=@id AND is_deleted=false",new{id});}
    public async Task<IReadOnlyList<FormDimensionReadModel>> GetDimensionsAsync(Guid formId){using var c=f.Create(); return (await c.QueryAsync<FormDimensionReadModel>("SELECT id,name,description,display_order AS DisplayOrder,max_score AS MaxScore FROM valorapesquisa.form_dimensions WHERE form_id=@formId ORDER BY display_order",new{formId})).ToList();}
    public async Task<IReadOnlyList<QuestionPublicReadModel>> GetQuestionsAsync(Guid formId){using var c=f.Create(); return (await c.QueryAsync<QuestionPublicReadModel>("SELECT id,form_id AS FormId,dimension_id AS DimensionId,text,type,required,COALESCE(max_score,5) AS MaxScore,display_order AS DisplayOrder FROM valorapesquisa.questions WHERE form_id=@formId ORDER BY display_order",new{formId})).ToList();}
    public async Task<IReadOnlyList<QuestionOptionPublicReadModel>> GetQuestionOptionsAsync(Guid formId){using var c=f.Create(); return (await c.QueryAsync<QuestionOptionPublicReadModel>("SELECT qo.id,qo.question_id AS QuestionId,qo.text,qo.score,qo.display_order AS DisplayOrder FROM valorapesquisa.question_options qo JOIN valorapesquisa.questions q ON q.id=qo.question_id WHERE q.form_id=@formId ORDER BY qo.display_order",new{formId})).ToList();}
}

using Valora.Application.ReadModels;
namespace Valora.Application.Contracts;
public interface IFormRepository { Task<FormPublicReadModel?> GetAsync(Guid id); Task<FormPublicReadModel?> GetByIdAsync(Guid id); Task<IReadOnlyList<FormDimensionReadModel>> GetDimensionsAsync(Guid formId); Task<IReadOnlyList<QuestionPublicReadModel>> GetQuestionsAsync(Guid formId); Task<IReadOnlyList<QuestionOptionPublicReadModel>> GetQuestionOptionsAsync(Guid formId); }

namespace Valora.Application.Contracts;
public interface IFormRepository { Task<dynamic?> GetAsync(Guid id); Task<dynamic?> GetByIdAsync(Guid id); Task<IReadOnlyList<dynamic>> GetDimensionsAsync(Guid formId); Task<IReadOnlyList<dynamic>> GetQuestionsAsync(Guid formId); Task<IReadOnlyList<dynamic>> GetQuestionOptionsAsync(Guid formId); }

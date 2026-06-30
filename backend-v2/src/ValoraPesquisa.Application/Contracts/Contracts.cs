using ValoraPesquisa.Domain.Entities;
namespace ValoraPesquisa.Application.Contracts;
public record CurrentUser(Guid Id,Guid? OrganizationId,string Role,string Email){ public bool IsAdminValora=>Role=="admin_valora"; }
public interface IAuditService { Task RecordAsync(Guid? orgId,Guid? userId,string action,string entity,Guid? entityId,string correlationId,object? metadata=null); }
public interface IPasswordHasher { string Hash(string password); bool Verify(string password,string hash); }
public interface ITokenHasher { string NewToken(); string Hash(string token); bool Verify(string token,string hash); }
public interface IUserRepository { Task<User?> FindByEmailAsync(string email); Task<User?> FindByIdAsync(Guid id); Task UpdateLastLoginAsync(Guid id); Task<IReadOnlyList<User>> ListAsync(CurrentUser user); Task<User> CreateAsync(User user); }
public interface IOrganizationRepository { Task<IReadOnlyList<Organization>> ListAsync(); Task<Organization> CreateAsync(Organization org); Task<Organization?> FindBySlugAsync(string slug); }
public interface IFormRepository { Task<IReadOnlyList<Form>> ListAsync(CurrentUser user); Task<Form?> GetAsync(Guid id,CurrentUser user); Task<Form> SaveAsync(Form form); Task<bool> HasQuestionsAsync(Guid formId); }
public interface ISurveyRepository { Task<IReadOnlyList<Survey>> ListAsync(CurrentUser user); Task<Survey?> GetAsync(Guid id,CurrentUser? user=null); Task<Survey> SaveAsync(Survey survey); Task SetStatusAsync(Guid id,string status,CurrentUser user); }
public interface ISurveyLinkRepository { Task<IReadOnlyList<SurveyLink>> ListAsync(Guid surveyId,CurrentUser user); Task<SurveyLink> CreateAsync(SurveyLink link); Task<SurveyLink?> FindValidAsync(Guid surveyId,string orgSlug); Task SetStatusAsync(Guid linkId,string status,CurrentUser user); }
public interface IResponseRepository { Task<SurveyResponse> CreateAsync(SurveyResponse response, ResultScore score); Task<ResultScore?> GetResultAsync(Guid responseId,string resultToken); }

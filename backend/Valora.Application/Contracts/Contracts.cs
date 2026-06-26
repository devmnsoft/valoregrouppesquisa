using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IOrganizationRepository { Task<dynamic?> GetAsync(Guid id); Task<Guid> CreateAsync(string name,string email,string slug,string planId); Task UpdateCurrentAsync(Guid id, UpdateOrganizationRequest request); Task<int> CountManagersAsync(Guid organizationId); }
public interface IUserRepository { Task<dynamic?> GetByEmailAsync(string email); Task<dynamic?> GetAsync(Guid id); Task<Guid> CreateAsync(Guid organizationId,string name,string email,string passwordHash,string role); Task TouchLoginAsync(Guid id); }
public interface IPlanRepository { Task<IReadOnlyList<PlanDto>> GetPublicPlansAsync(); Task<PlanDto?> GetByIdAsync(string id); Task<string?> GetCurrentPlanIdAsync(Guid organizationId); Task CreateSubscriptionAsync(Guid organizationId,string planId); }
public interface ISurveyRepository { Task<dynamic?> GetPublicByTokenAsync(string token); Task<Guid> SaveResponseAsync(Guid surveyId, SubmitResponseRequest request); Task<dynamic?> GetResultAsync(Guid responseId); Task<int> CountActiveSurveysAsync(Guid organizationId); Task<int> CountResponsesThisMonthAsync(Guid organizationId); }
public interface IResponseRepository { Task<dynamic?> GetResultAsync(Guid responseId); }
public interface IAuditRepository { Task AddAsync(AuditEntry entry); }
public interface IJwtTokenService { string CreateToken(Guid userId, Guid? organizationId, string email, string role); }
public interface IPasswordHasher { string Hash(string password); bool Verify(string password,string hash); }

public interface IDbConnectionFactory { System.Data.IDbConnection Create(); }
public interface IFormRepository { Task<dynamic?> GetAsync(Guid id); }
public interface IResultRepository { Task<dynamic?> GetByResponseAsync(Guid responseId); }
public interface ICertificateRepository { Task<dynamic?> GetByResponseAsync(Guid responseId); Task CreateMetadataAsync(Guid responseId,string validationCode,string status); }
public interface ICommunicationRepository { Task AddEmailJobAsync(Guid responseId,string toEmail,string status); Task<IReadOnlyList<dynamic>> ListAsync(Guid? organizationId=null); }
public interface IMigrationRepository { Task<dynamic?> GetStatusAsync(); Task SaveCompareReportAsync(string reportJson); }

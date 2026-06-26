using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface ICommunicationRepository { Task AddEmailJobAsync(Guid responseId,string toEmail,string status); Task<IReadOnlyList<dynamic>> ListAsync(Guid? organizationId=null); }

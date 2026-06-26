using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IUserRepository { Task<dynamic?> GetByEmailAsync(string email); Task<dynamic?> GetAsync(Guid id); Task<Guid> CreateAsync(Guid organizationId,string name,string email,string passwordHash,string role); Task TouchLoginAsync(Guid id); }

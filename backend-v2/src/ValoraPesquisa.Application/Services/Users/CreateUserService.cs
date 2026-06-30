using ValoraPesquisa.Application.Contracts; using ValoraPesquisa.Application.DTOs; using ValoraPesquisa.Domain.Entities;
namespace ValoraPesquisa.Application.Services.Users;
public sealed class CreateUserService(IUserRepository repo,IPasswordHasher hasher){ public Task<UserDto> ExecuteAsync(Guid id,Guid? org,string name,string email,string password,string role,string? phone)=>repo.CreateAsync(new User(id,org,name,email,hasher.Hash(password),role,"active",phone,null,DateTimeOffset.UtcNow,null,false)); }

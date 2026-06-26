using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IFormRepository { Task<dynamic?> GetAsync(Guid id); }

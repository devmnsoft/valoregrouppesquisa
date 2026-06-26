using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IResultRepository { Task<dynamic?> GetByResponseAsync(Guid responseId); }

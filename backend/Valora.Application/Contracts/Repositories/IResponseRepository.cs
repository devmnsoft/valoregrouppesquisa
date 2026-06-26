using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IResponseRepository { Task<dynamic?> GetResultAsync(Guid responseId); }

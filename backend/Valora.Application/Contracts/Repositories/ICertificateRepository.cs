using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface ICertificateRepository { Task<dynamic?> GetByResponseAsync(Guid responseId); Task CreateMetadataAsync(Guid responseId,string validationCode,string status); }

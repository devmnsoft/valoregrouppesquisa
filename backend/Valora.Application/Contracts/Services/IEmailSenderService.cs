using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IEmailSenderService { Task<int> ProcessAsync(int batchSize=20); }

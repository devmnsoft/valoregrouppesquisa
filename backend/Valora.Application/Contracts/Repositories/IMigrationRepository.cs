using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IMigrationRepository { Task<dynamic?> GetStatusAsync(); Task SaveCompareReportAsync(string reportJson); }

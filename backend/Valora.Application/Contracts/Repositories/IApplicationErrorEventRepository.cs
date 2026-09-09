namespace Valora.Application.Contracts;

public interface IApplicationErrorEventRepository
{
    Task AddAsync(Guid? organizationId, string source, string severity, string friendlyMessage,
        string technicalDetail, string correlationId, CancellationToken cancellationToken);
}

using System.Data;

namespace Valora.Application.Contracts;

public interface IDbTransactionFactory
{
    Task<IUnitOfWork> BeginAsync(CancellationToken cancellationToken = default);
}

using System.Data;

namespace Valora.Application.Contracts;

public interface IUnitOfWork : IAsyncDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }
    CancellationToken CancellationToken { get; }
    Task CommitAsync();
    Task RollbackAsync();
}

public interface IDbTransactionFactory
{
    Task<IUnitOfWork> BeginAsync(CancellationToken cancellationToken = default);
}

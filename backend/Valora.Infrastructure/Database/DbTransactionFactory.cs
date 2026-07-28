using System.Data.Common;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Database;

public sealed class DbTransactionFactory(IDbConnectionFactory connections) : IDbTransactionFactory
{
    public Task<IUnitOfWork> BeginAsync(CancellationToken cancellationToken = default)
    {
        var connection = connections.Create() as DbConnection
            ?? throw new InvalidOperationException("A conexão configurada não suporta transações assíncronas.");
        return BeginCoreAsync(connection, cancellationToken);
    }

    private static async Task<IUnitOfWork> BeginCoreAsync(DbConnection connection, CancellationToken cancellationToken) =>
        await DapperUnitOfWork.BeginAsync(connection, cancellationToken);
}

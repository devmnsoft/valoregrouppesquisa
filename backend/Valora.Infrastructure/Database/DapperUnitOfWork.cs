using System.Data;
using System.Data.Common;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Database;

public sealed class DapperUnitOfWork : IUnitOfWork
{
    private readonly DbConnection connection;
    private readonly DbTransaction transaction;
    private bool completed;

    private DapperUnitOfWork(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)
    {
        this.connection = connection;
        this.transaction = transaction;
        CancellationToken = cancellationToken;
    }

    public IDbConnection Connection => connection;
    public IDbTransaction Transaction => transaction;
    public CancellationToken CancellationToken { get; }

    public static async Task<DapperUnitOfWork> BeginAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await connection.OpenAsync(cancellationToken);
        return new DapperUnitOfWork(connection, await connection.BeginTransactionAsync(cancellationToken), cancellationToken);
    }

    public async Task CommitAsync()
    {
        await transaction.CommitAsync(CancellationToken);
        completed = true;
    }

    public async Task RollbackAsync()
    {
        if (completed) return;
        await transaction.RollbackAsync(CancellationToken);
        completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!completed) await RollbackAsync();
        await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }
}

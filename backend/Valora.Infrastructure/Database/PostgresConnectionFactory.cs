using System.Data; using Microsoft.Extensions.Configuration; using Npgsql; using Valora.Application.Contracts;
namespace Valora.Infrastructure.Database;

public sealed class PostgresConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    public IDbConnection Create()
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Postgres não foi configurada.");

        return new NpgsqlConnection(connectionString);
    }
}

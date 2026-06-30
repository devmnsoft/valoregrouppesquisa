using System.Data; using Npgsql;
namespace ValoraPesquisa.Infrastructure.Database;
public sealed class PostgresConnectionFactory(IConfigurationLike config){ public IDbConnection Create()=>new NpgsqlConnection(config.ConnectionString); }
public interface IConfigurationLike { string ConnectionString { get; } }

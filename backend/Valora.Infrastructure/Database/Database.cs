using System.Data; using Dapper; using Microsoft.Extensions.Configuration; using Microsoft.Extensions.Logging; using Npgsql;
namespace Valora.Infrastructure.Database;
public interface IDbConnectionFactory{ IDbConnection Create(); }
public sealed class PostgresConnectionFactory(IConfiguration cfg):IDbConnectionFactory{ public IDbConnection Create()=>new NpgsqlConnection(cfg.GetConnectionString("DefaultConnection")); }

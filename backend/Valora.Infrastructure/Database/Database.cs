using System.Data; using Microsoft.Extensions.Configuration; using Npgsql; using Valora.Application.Contracts;
namespace Valora.Infrastructure.Database;
public sealed class PostgresConnectionFactory(IConfiguration cfg):IDbConnectionFactory{ public IDbConnection Create()=>new NpgsqlConnection(cfg.GetConnectionString("DefaultConnection")); }

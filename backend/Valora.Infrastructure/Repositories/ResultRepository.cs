using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Infrastructure.Database;

namespace Valora.Infrastructure.Repositories;
public sealed class ResultRepository(IDbConnectionFactory f):IResultRepository{ public async Task<dynamic?> GetByResponseAsync(Guid responseId){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync("SELECT * FROM valorapesquisa.result_scores WHERE response_id=@responseId",new{responseId});}}

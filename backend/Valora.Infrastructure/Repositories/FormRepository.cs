using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Infrastructure.Database;

namespace Valora.Infrastructure.Repositories;
public sealed class FormRepository(IDbConnectionFactory f):IFormRepository{ public async Task<dynamic?> GetAsync(Guid id){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync("SELECT * FROM valorapesquisa.forms WHERE id=@id AND is_deleted=false",new{id});}}

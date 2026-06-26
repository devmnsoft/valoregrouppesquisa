using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Infrastructure.Database;

namespace Valora.Infrastructure.Repositories;
public sealed class CertificateRepository(IDbConnectionFactory f):ICertificateRepository{ public async Task<dynamic?> GetByResponseAsync(Guid responseId){using var c=f.Create(); return await c.QuerySingleOrDefaultAsync("SELECT * FROM valorapesquisa.certificates WHERE response_id=@responseId",new{responseId});} public async Task CreateMetadataAsync(Guid responseId,string validationCode,string status){using var c=f.Create(); await c.ExecuteAsync("INSERT INTO valorapesquisa.certificates(response_id,certificate_code,issuer_name) VALUES (@responseId,@validationCode,@status) ON CONFLICT (certificate_code) DO UPDATE SET updated_at=now()",new{responseId,validationCode,status});}}

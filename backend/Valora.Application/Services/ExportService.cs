using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

internal static class SafeData { public static string MaskEmail(string? email){ if(string.IsNullOrWhiteSpace(email)||!email.Contains('@')) return "***"; var p=email.Split('@'); return $"{p[0][0]}***@{p[1]}"; } public static string Hash(string? value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value??""))).ToLowerInvariant(); }

public sealed class ExportService(IExportRepository repo,IEntitlementService ent,IAuditRepository audit):IExportService{ static readonly string[] Allowed={"responses","results","audit","surveys","forms"}; public async Task<ExportJobDto> RequestAsync(Guid o,Guid? u,ExportRequest req){ if(!await ent.CanUseAsync(o,"exportacoes")) throw new InvalidOperationException("MODULE_NOT_ENABLED"); if(!Allowed.Contains(req.Entity)) throw new InvalidOperationException("INVALID_EXPORT_ENTITY"); var j=await repo.CreateAsync(o,u,req.Entity,req.Format,req.FilterJson); var payload=req.Format=="json"?"[]":"id,status\n"; await repo.CompleteAsync(o,j.Id,$"{req.Entity}.{req.Format}",req.Format=="json"?"application/json":"text/csv",payload); await audit.AddAsync(new AuditEntry(o,u,"export.completed","export",j.Id.ToString(),"Exportação concluída","{}")); return (await repo.GetAsync(o,j.Id))!;} public Task<IReadOnlyList<ExportJobDto>> ListAsync(Guid o)=>repo.ListAsync(o); public Task<ExportJobDto?> GetAsync(Guid o,Guid id)=>repo.GetAsync(o,id); }

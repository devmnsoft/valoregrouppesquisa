using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

internal static class SafeData { public static string MaskEmail(string? email){ if(string.IsNullOrWhiteSpace(email)||!email.Contains('@')) return "***"; var p=email.Split('@'); return $"{p[0][0]}***@{p[1]}"; } public static string Hash(string? value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value??""))).ToLowerInvariant(); }

public sealed class ReportService(IReportRepository repo, ReportBuilderService builder, IEntitlementService ent, IAuditRepository audit) : IReportService { async Task Check(Guid org){ if(!await ent.CanUseAsync(org,"relatorios")) throw new InvalidOperationException("MODULE_NOT_ENABLED"); } public async Task<GeneratedReportDto> GenerateSurveyAsync(Guid o,Guid s,string f,Guid? u){ await Check(o); var p=await builder.BuildAsync(o,s,null,f); var r=await repo.CreateGeneratedAsync(o,s,null,null,"Relatório da pesquisa",f,p,u); await audit.AddAsync(new AuditEntry(o,u,"report.generated","survey",s.ToString(),"Relatório gerado","{}")); return r;} public async Task<GeneratedReportDto> GenerateResponseAsync(Guid o,Guid rId,string f,Guid? u){ await Check(o); var p=await builder.BuildAsync(o,null,rId,f); var r=await repo.CreateGeneratedAsync(o,null,rId,null,"Relatório da resposta",f,p,u); await audit.AddAsync(new AuditEntry(o,u,"report.generated","response",rId.ToString(),"Relatório gerado","{}")); return r;} public async Task<GeneratedReportDto> GenerateOrganizationAsync(Guid o,string f,Guid? u){ await Check(o); var p=await builder.BuildAsync(o,null,null,f); return await repo.CreateGeneratedAsync(o,null,null,null,"Relatório executivo",f,p,u);} public Task<IReadOnlyList<GeneratedReportDto>> ListGeneratedAsync(Guid o)=>repo.ListGeneratedAsync(o); public Task<GeneratedReportDto?> GetGeneratedAsync(Guid o,Guid id)=>repo.GetGeneratedAsync(o,id); }

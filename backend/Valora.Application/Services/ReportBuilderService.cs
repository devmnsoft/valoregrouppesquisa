using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;

internal static class SafeData { public static string MaskEmail(string? email){ if(string.IsNullOrWhiteSpace(email)||!email.Contains('@')) return "***"; var p=email.Split('@'); return $"{p[0][0]}***@{p[1]}"; } public static string Hash(string? value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value??""))).ToLowerInvariant(); }

public sealed class ReportBuilderService(IResponseRepository responses, ISurveyRepository surveys, IOrganizationRepository orgs){ public async Task<string> BuildAsync(Guid organizationId,Guid? surveyId,Guid? responseId,string format){ var data=new Dictionary<string,object?>{{"organization",await orgs.GetAsync(organizationId)}}; if(surveyId.HasValue) data["survey"]=await surveys.GetAdminAsync(organizationId,surveyId.Value); if(responseId.HasValue) data["response"]=await responses.GetAdminAsync(organizationId,responseId.Value); data["generatedAt"]=DateTimeOffset.UtcNow; return format=="csv"?"tipo,id\nrelatorio,"+(responseId??surveyId??organizationId):JsonSerializer.Serialize(data); }}

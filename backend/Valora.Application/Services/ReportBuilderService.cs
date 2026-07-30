using System.Text.Json;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class ReportBuilderService(IResponseRepository responses, ISurveyRepository surveys, IOrganizationRepository orgs){ public async Task<string> BuildAsync(Guid organizationId,Guid? surveyId,Guid? responseId,string format){ var data=new Dictionary<string,object?>{{"organization",await orgs.GetAsync(organizationId)}}; if(surveyId.HasValue) data["survey"]=await surveys.GetAdminAsync(organizationId,surveyId.Value); if(responseId.HasValue) data["response"]=await responses.GetAdminAsync(organizationId,responseId.Value); data["generatedAt"]=DateTimeOffset.UtcNow; return format=="csv"?"tipo,id\nrelatorio,"+(responseId??surveyId??organizationId):JsonSerializer.Serialize(data); }}

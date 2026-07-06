using Microsoft.Extensions.Logging;
using ValoraPesquisa.Domain.Attachments;
using ValoraPesquisa.Domain.Automation;
using ValoraPesquisa.Domain.Forms;
using ValoraPesquisa.Domain.Notifications;
using ValoraPesquisa.Domain.Reports;
namespace ValoraPesquisa.Application.V11;
public sealed record Result<T>(bool Success, T? Value, string? Error){ public static Result<T> Ok(T value)=>new(true,value,null); public static Result<T> Fail(string error)=>new(false,default,error); }
public sealed record V11Context(Guid TenantId, Guid UserId, IReadOnlySet<string> Permissions);
public interface IFormDefinitionRepository { Task<IReadOnlyList<FormDefinition>> ListAsync(Guid tenantId,CancellationToken ct); Task<FormDefinition?> GetAsync(Guid tenantId,Guid id,CancellationToken ct); Task SaveAsync(FormDefinition definition,CancellationToken ct); }
public interface IFormResponseRepository { Task SaveAsync(FormResponse response,IReadOnlyList<FormResponseField> fields,CancellationToken ct); Task<FormResponse?> GetAsync(Guid tenantId,Guid id,CancellationToken ct); }
public interface IFormBuilderService { Result<FormField> BuildField(FormField field); }
public interface IFormValidationService { Result<bool> Validate(IReadOnlyList<FormField> fields,IReadOnlyDictionary<Guid,string?> values); }
public interface IFormRendererService { Result<string> RenderPreview(FormDefinition definition,IReadOnlyList<FormField> fields); }
public interface IFormPublisher { Task<Result<FormVersion>> PublishAsync(V11Context context,Guid formId,CancellationToken ct); }
public interface IAutomationRuleRepository { Task SaveAsync(AutomationRule rule,CancellationToken ct); Task<AutomationRule?> GetAsync(Guid tenantId,Guid id,CancellationToken ct); Task<IReadOnlyList<AutomationRule>> ListAsync(Guid tenantId,CancellationToken ct); }
public interface IAutomationExecutionRepository { Task SaveAsync(AutomationExecution execution,CancellationToken ct); Task LogAsync(AutomationExecutionLog log,CancellationToken ct); }
public interface IAutomationEngine { Task<Result<AutomationExecution>> ExecuteAsync(V11Context context,Guid ruleId,CancellationToken ct); }
public interface IAutomationConditionEvaluator { Result<bool> Evaluate(AutomationCondition condition,IReadOnlyDictionary<string,string?> payload); }
public interface IAutomationActionExecutor { Task<Result<bool>> ExecuteAsync(AutomationAction action,V11Context context,CancellationToken ct); }
public interface IAutomationScheduler { Task TickAsync(CancellationToken ct); }
public interface IAttachmentRepository { Task SaveAsync(AttachmentFile file,CancellationToken ct); Task<AttachmentFile?> GetAsync(Guid tenantId,Guid id,CancellationToken ct); Task LinkAsync(AttachmentLink link,CancellationToken ct); }
public interface IAttachmentStorageService { Task<string> SaveAsync(Stream stream,string fileName,CancellationToken ct); Task<Stream> OpenReadAsync(string storageKey,CancellationToken ct); }
public interface IAttachmentValidationService { Result<bool> Validate(string extension,long sizeBytes); }
public interface IAttachmentAccessService { Result<bool> CanRead(V11Context context,AttachmentFile file); }
public interface IAttachmentHashService { Task<string> Sha256Async(Stream stream,CancellationToken ct); }
public interface INotificationRepository { Task SaveAsync(Notification notification,IReadOnlyList<NotificationRecipient> recipients,CancellationToken ct); Task<IReadOnlyList<NotificationRecipient>> ListForUserAsync(Guid tenantId,Guid userId,CancellationToken ct); }
public interface INotificationService { Task<Result<Notification>> SendAsync(V11Context context,string eventCode,string title,string body,IReadOnlyList<Guid> recipients,CancellationToken ct); }
public interface INotificationPreferenceService { Task<Result<bool>> UpdateAsync(V11Context context,NotificationChannel channel,bool enabled,CancellationToken ct); }
public interface IPushNotificationSender { Task<Result<bool>> SendFakeAsync(NotificationRecipient recipient,CancellationToken ct); }
public interface IReportDefinitionRepository { Task SaveAsync(ReportDefinition definition,CancellationToken ct); Task<IReadOnlyList<ReportDefinition>> ListAsync(Guid tenantId,CancellationToken ct); }
public interface IReportExecutionRepository { Task SaveAsync(ReportExecution execution,CancellationToken ct); Task SaveExportAsync(ReportExport export,CancellationToken ct); }
public interface IReportExportService { Result<byte[]> ExportCsv(IReadOnlyList<IReadOnlyDictionary<string,object?>> rows,bool bom=true); Result<string> ExportJson(IReadOnlyList<IReadOnlyDictionary<string,object?>> rows); Result<string> ExportPdfHtml(IReadOnlyList<IReadOnlyDictionary<string,object?>> rows); }
public interface IReportQueryService { Task<Result<IReadOnlyList<IReadOnlyDictionary<string,object?>>>> ExecuteAsync(V11Context context,string queryKey,IReadOnlyDictionary<string,string?> filters,CancellationToken ct); }
public interface IReportScheduler { Task TickAsync(CancellationToken ct); }
public abstract class V11UseCase<TRequest,TResponse>(ILogger logger){ protected Result<TResponse> Deny(string permission)=>Result<TResponse>.Fail($"Permissão necessária: {permission}"); protected bool Has(V11Context ctx,string permission)=>ctx.Permissions.Contains(permission); protected Result<TResponse> Error(Exception ex){ logger.LogError(ex,"Falha no caso de uso v1.1"); return Result<TResponse>.Fail("Erro interno ao executar operação v1.1."); } public abstract Task<Result<TResponse>> ExecuteAsync(V11Context context,TRequest request,CancellationToken ct); }
public sealed record CreateFormDefinitionRequest(string Name,string? Description);
public sealed class CreateFormDefinitionUseCase(IFormDefinitionRepository repo,ILogger<CreateFormDefinitionUseCase> log):V11UseCase<CreateFormDefinitionRequest,FormDefinition>(log){ public override async Task<Result<FormDefinition>> ExecuteAsync(V11Context c,CreateFormDefinitionRequest r,CancellationToken ct){ try{ if(!Has(c,"forms.criar")) return Deny("forms.criar"); var f=new FormDefinition(Guid.NewGuid(),c.TenantId,r.Name,r.Description,false); await repo.SaveAsync(f,ct); return Result<FormDefinition>.Ok(f);}catch(Exception ex){return Error(ex);} } }
public sealed record ExecuteAutomationRuleRequest(Guid RuleId);
public sealed class ExecuteAutomationRuleUseCase(IAutomationEngine engine,ILogger<ExecuteAutomationRuleUseCase> log):V11UseCase<ExecuteAutomationRuleRequest,AutomationExecution>(log){ public override async Task<Result<AutomationExecution>> ExecuteAsync(V11Context c,ExecuteAutomationRuleRequest r,CancellationToken ct){ try{ if(!Has(c,"automation.rules.executar")) return Deny("automation.rules.executar"); return await engine.ExecuteAsync(c,r.RuleId,ct);}catch(Exception ex){return Error(ex);} } }
public sealed record ExportReportRequest(string QueryKey,IReadOnlyDictionary<string,string?> Filters,ReportFormat Format);
public sealed class ExportReportUseCase(IReportQueryService query,IReportExportService export,ILogger<ExportReportUseCase> log):V11UseCase<ExportReportRequest,byte[]>(log){ public override async Task<Result<byte[]>> ExecuteAsync(V11Context c,ExportReportRequest r,CancellationToken ct){ try{ if(!Has(c,"reports.exportar")) return Deny("reports.exportar"); var rows=await query.ExecuteAsync(c,r.QueryKey,r.Filters,ct); if(!rows.Success||rows.Value is null) return Result<byte[]>.Fail(rows.Error??"Relatório inválido."); return r.Format==ReportFormat.Csv?export.ExportCsv(rows.Value):Result<byte[]>.Fail("Formato documentado para evolução: xlsx/pdf_html/json via armazenamento de exportações.");}catch(Exception ex){return Error(ex);} } }

namespace Valora.Application.Processes;

internal static class ProcessScope
{
    public static void Ensure(Guid organizationId) { if (organizationId == Guid.Empty) throw new ArgumentException("Selecione uma organização para continuar."); }
    public static void EnsureId(Guid id, string field) { if (id == Guid.Empty) throw new ArgumentException($"{field} é obrigatório."); }
}
public sealed class ProcessDefinitionService(IProcessDefinitionRepository repository, IProcessStepRepository steps)
{
    public Task<IReadOnlyList<ProcessDefinitionDto>> List(Guid organizationId,CancellationToken ct){ProcessScope.Ensure(organizationId);return repository.List(organizationId,ct);}
    public Task<ProcessDefinitionDto?> Get(Guid organizationId,Guid id,CancellationToken ct){ProcessScope.Ensure(organizationId);ProcessScope.EnsureId(id,"Processo");return repository.Get(organizationId,id,ct);}
    public Task<Guid> Create(Guid organizationId,Guid userId,CreateProcessRequest request,CancellationToken ct){ProcessScope.Ensure(organizationId);ProcessScope.EnsureId(userId,"Usuário");if(request.OwnerUserId==Guid.Empty)throw new ArgumentException("Defina o dono do processo.");return repository.Create(organizationId,userId,request,ct);}
    public async Task Publish(Guid organizationId,Guid id,Guid userId,CancellationToken ct){ProcessScope.Ensure(organizationId);ProcessScope.EnsureId(id,"Processo");ProcessScope.EnsureId(userId,"Usuário");var definition=await repository.Get(organizationId,id,ct)??throw new KeyNotFoundException("Processo não encontrado.");if(definition.Status=="published")return;var items=await steps.List(organizationId,id,ct);if(!items.Any(x=>x.StepType=="initial")||!items.Any(x=>x.StepType=="final"))throw new InvalidOperationException("Este processo ainda não possui etapas suficientes para publicação.");await repository.Publish(organizationId,id,userId,ct);}
    public Task<Guid> NewVersion(Guid organizationId,Guid id,Guid userId,CancellationToken ct){ProcessScope.Ensure(organizationId);ProcessScope.EnsureId(id,"Processo");ProcessScope.EnsureId(userId,"Usuário");return repository.NewVersion(organizationId,id,userId,ct);}
}
public sealed class ProcessStepService(IProcessStepRepository repository,IProcessDefinitionRepository definitions)
{
    public Task<IReadOnlyList<ProcessStepDto>> List(Guid organizationId,Guid processId,CancellationToken ct){ProcessScope.Ensure(organizationId);ProcessScope.EnsureId(processId,"Processo");return repository.List(organizationId,processId,ct);}
    public async Task<Guid> Create(Guid organizationId,Guid processId,CreateProcessStepRequest request,CancellationToken ct){ProcessScope.Ensure(organizationId);ProcessScope.EnsureId(processId,"Processo");var process=await definitions.Get(organizationId,processId,ct)??throw new KeyNotFoundException("Processo não encontrado.");if(process.Status=="published")throw new InvalidOperationException("Crie uma nova versão para alterar um processo publicado.");return await repository.Create(organizationId,processId,request,ct);}
}
public sealed class ProcessInstanceService(IProcessInstanceRepository repository)
{
    public Task<ProcessDashboardDto> Dashboard(Guid o,CancellationToken ct){ProcessScope.Ensure(o);return repository.Dashboard(o,ct);} public Task<IReadOnlyList<ProcessInstanceDto>> List(Guid o,CancellationToken ct){ProcessScope.Ensure(o);return repository.List(o,ct);} public Task<ProcessInstanceDto?> Get(Guid o,Guid id,CancellationToken ct){ProcessScope.Ensure(o);return repository.Get(o,id,ct);}
    public Task<Guid> Create(Guid o,Guid u,CreateProcessInstanceRequest r,CancellationToken ct){ProcessScope.Ensure(o);if(r.ResponsibleUserId==Guid.Empty)throw new ArgumentException("Defina o responsável pela execução.");return repository.Create(o,u,r,ct);}
    public Task ChangeState(Guid o,Guid id,Guid u,string operation,bool evidence,CancellationToken ct){ProcessScope.Ensure(o);return repository.ChangeState(o,id,u,operation,evidence,ct);}
}
public sealed class ProcessApprovalService(IProcessApprovalRepository repository){public Task Decide(Guid o,Guid id,Guid u,string decision,ApprovalDecisionRequest r,CancellationToken ct){ProcessScope.Ensure(o);if(decision=="rejected"&&string.IsNullOrWhiteSpace(r.Justification))throw new ArgumentException("Informe a justificativa da reprovação.");if(decision=="returned"&&!r.ReturnStepId.HasValue)throw new ArgumentException("Selecione a etapa de retorno.");return repository.Decide(o,id,u,decision,r,ct);}}
public sealed class ProcessSlaService(IProcessSlaRepository repository){public Task<IReadOnlyList<ProcessSlaDto>> List(Guid o,CancellationToken ct){ProcessScope.Ensure(o);return repository.List(o,ct);}}
public sealed class ProcessAutomationService(IProcessAutomationRepository repository){public Task Record(Guid o,Guid rule,string status,string? detail,CancellationToken ct){ProcessScope.Ensure(o);return repository.RecordExecution(o,rule,status,detail,ct);}}
public sealed class ProcessBottleneckInsightService(IProcessInsightRepository repository){public Task<IReadOnlyList<ProcessInsightDto>> List(Guid o,CancellationToken ct){ProcessScope.Ensure(o);return repository.List(o,ct);}}
public sealed class ProcessTemplateService(IProcessTemplateRepository repository){public Task<IReadOnlyList<ProcessTemplateDto>> List(Guid o,CancellationToken ct){ProcessScope.Ensure(o);return repository.List(o,ct);}}

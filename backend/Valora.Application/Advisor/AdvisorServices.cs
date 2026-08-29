namespace Valora.Application.Advisor;

internal static class AdvisorScope
{
    internal static Guid Organization(Guid id) => id == Guid.Empty ? throw new InvalidOperationException("Selecione uma organização antes de acessar o Valora Advisor™.") : id;
    internal static Guid User(Guid id) => id == Guid.Empty ? throw new InvalidOperationException("Não foi possível identificar o usuário autenticado.") : id;
}

public sealed class AdvisorConversationService(IAdvisorConversationRepository repository, AdvisorUsageService usage)
{
    public Task<IReadOnlyList<AdvisorConversationDto>> List(Guid o, Guid u, CancellationToken ct) => repository.List(AdvisorScope.Organization(o), AdvisorScope.User(u), ct);
    public Task<AdvisorConversationDetailDto?> Get(Guid o, Guid u, Guid id, CancellationToken ct) => repository.Get(AdvisorScope.Organization(o), AdvisorScope.User(u), id, ct);
    public async Task<Guid> Create(Guid o, Guid u, CreateAdvisorConversationRequest request, CancellationToken ct) { o=AdvisorScope.Organization(o);u=AdvisorScope.User(u);var id=await repository.Create(o,u,request,ct);await usage.Record(o,u,"advisor.conversation.created",id,ct);return id; }
}
public sealed class AdvisorContextBuilderService(IAdvisorContextBundleRepository repository)
{
    public Task<IReadOnlyList<AdvisorContextOptionDto>> Options(Guid o,CancellationToken ct)=>repository.Options(AdvisorScope.Organization(o),ct);
    public Task<IReadOnlyList<AdvisorContextOptionDto>> Build(Guid o,IReadOnlyList<AdvisorContextSelection> selections,CancellationToken ct)=>repository.Resolve(AdvisorScope.Organization(o),selections,ct);
}
public sealed class AdvisorGuardrailService(IAdvisorGuardrailRepository repository)
{
    public async Task Validate(Guid o,Guid u,Guid? conversation,IReadOnlyList<AdvisorContextOptionDto> evidence,CancellationToken ct)
    { if(evidence.Count>0)return;await repository.Record(o,u,conversation,"evidence.required","Uma resposta analítica foi bloqueada porque nenhuma fonte verificável foi selecionada.",ct);throw new InvalidOperationException("Dados insuficientes: selecione ao menos uma fonte verificável. O Advisor não conclui sem evidência."); }
}
public sealed class AdvisorResponseComposerService(IAdvisorModelProvider provider)
{
    private const string Methodology = "A Metodologia Valora prevalece. Não invente dados, estatísticas ou benchmarks; diferencie evidência, inferência e hipótese; não trate sintoma como causa; cite fontes; indique limitações e preserve a decisão humana.";
    public async Task<AdvisorModelResult> Compose(string question,IReadOnlyList<AdvisorContextOptionDto> evidence,CancellationToken ct)
    { var result=await provider.Generate(new(Methodology,question,evidence),ct);if(result.ProviderUsed)return result;var sources=string.Join("; ",evidence.Select(x=>$"{x.Title} ({x.SourceType})"));return result with { Content=$"Leitura orientadora baseada nas fontes selecionadas: {sources}. As evidências permitem organizar a investigação, mas não demonstram causalidade isoladamente. Próximo passo recomendado: validar a interpretação com os responsáveis e reunir evidências convergentes antes de decidir." }; }
}
public sealed class AdvisorMessageService(IAdvisorMessageRepository messages,AdvisorContextBuilderService context,AdvisorGuardrailService guardrails,AdvisorResponseComposerService composer,AdvisorUsageService usage)
{
    public async Task<AdvisorMessageDto> Send(Guid o,Guid u,Guid conversation,SendAdvisorMessageRequest request,CancellationToken ct)
    { o=AdvisorScope.Organization(o);u=AdvisorScope.User(u);var evidence=await context.Build(o,request.Context,ct);await guardrails.Validate(o,u,conversation,evidence,ct);var userMessage=await messages.AddUserMessage(o,u,conversation,request.Content,ct);await usage.Record(o,u,"advisor.message.sent",userMessage,ct);var generated=await composer.Compose(request.Content,evidence,ct);var limitations=new[]{generated.Limitation??"A associação observada não comprova causalidade; valide a leitura com pessoas responsáveis antes de decidir.","Esta orientação não substitui decisão humana."};var confidence=evidence.Count>=2?"moderate":"low";var id=await messages.AddResponse(o,conversation,generated.Content,confidence,limitations,evidence,ct);await usage.Record(o,u,"advisor.response.generated",id,ct);return new(id,conversation,"advisor",generated.Content,confidence,limitations,DateTimeOffset.UtcNow,evidence.Select(x=>new AdvisorEvidenceDto(Guid.Empty,x.SourceType,x.SourceId,x.Title,x.Summary,"contextual")).ToArray()); }
}
public sealed class AdvisorPromptTemplateService(IAdvisorPromptTemplateRepository repository){public Task<IReadOnlyList<AdvisorTemplateDto>> List(Guid o,CancellationToken ct)=>repository.List(AdvisorScope.Organization(o),ct);public Task<Guid>Create(Guid o,Guid u,CreateAdvisorTemplateRequest r,CancellationToken ct)=>repository.Create(AdvisorScope.Organization(o),AdvisorScope.User(u),r,ct);}
public sealed class AdvisorFeedbackService(IAdvisorFeedbackRepository repository,AdvisorUsageService usage){public async Task Create(Guid o,Guid u,Guid message,AdvisorFeedbackRequest r,CancellationToken ct){o=AdvisorScope.Organization(o);u=AdvisorScope.User(u);await repository.Create(o,u,message,r,ct);await usage.Record(o,u,"advisor.feedback.created",message,ct);}}
public sealed class AdvisorUsageService(IAdvisorUsageRepository repository){public Task Record(Guid o,Guid u,string name,Guid? id,CancellationToken ct)=>repository.Record(o,u,name,id,ct);}
public sealed class AdvisorEvidenceCitationService { public bool HasTraceableEvidence(IReadOnlyCollection<AdvisorEvidenceDto> evidence)=>evidence.Count>0&&evidence.All(x=>x.SourceId!=Guid.Empty); }
public sealed class AdvisorRecommendationService { public string RequireHumanConfirmation(string suggestion)=>$"Sugestão (requer confirmação humana): {suggestion}"; }
public sealed class AdvisorActionSuggestionService { public void EnsureConfirmation(bool confirmed){if(!confirmed)throw new InvalidOperationException("Confirme a conversão antes de criar uma ação ou decisão.");} }

public sealed class DisabledAdvisorModelProvider : IAdvisorModelProvider
{
    public Task<AdvisorModelResult> Generate(AdvisorModelRequest request,CancellationToken ct)=>Task.FromResult(new AdvisorModelResult(false,"","O provedor de IA não está configurado. Foi aplicada uma leitura determinística, limitada às fontes selecionadas."));
}

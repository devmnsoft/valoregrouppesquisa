using System.ComponentModel.DataAnnotations;
using Valora.Application.Advisor;

namespace Valora.Web.Models.ViewModels;

public sealed class AdvisorIndexViewModel
{
    public IReadOnlyList<AdvisorConversationDto> Conversations { get; init; }=[];
    public AdvisorConversationDetailDto? ActiveConversation { get; init; }
    public IReadOnlyList<AdvisorContextOptionDto> ContextOptions { get; init; }=[];
    public AdvisorAskForm Ask { get; init; }=new();
}
public sealed class AdvisorAskForm
{
    public Guid? ConversationId { get; init; }
    [Required,StringLength(300,MinimumLength=5)] public string Objective { get; init; }="";
    [Required,StringLength(4000,MinimumLength=3)] public string Question { get; init; }="";
    [MinLength(1,ErrorMessage="Selecione ao menos uma fonte verificável.")] public List<string> SourceKeys { get; init; }=[];
}
public sealed record AdvisorListPageViewModel(string Title,string Description,IReadOnlyList<AdvisorConversationDto> Conversations,IReadOnlyList<AdvisorTemplateDto> Templates);

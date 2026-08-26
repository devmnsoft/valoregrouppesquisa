using System.ComponentModel.DataAnnotations;
namespace Valora.Web.Models.ViewModels.SuccessCenter;
public sealed record SuccessMetric(string Label,string Value,string Detail,string Tone="neutral");
public sealed record SuccessCenterPageViewModel(string Eyebrow,string Title,string Description,string Section,IReadOnlyList<SuccessMetric> Metrics);
public sealed class CreateSupportTicketViewModel
{
 [Required(ErrorMessage="Informe o assunto."),StringLength(180,MinimumLength=5,ErrorMessage="Use entre 5 e 180 caracteres."),Display(Name="Assunto")] public string Subject{get;set;}="";
 [Required(ErrorMessage="Descreva o contexto."),StringLength(4000,MinimumLength=10,ErrorMessage="Use entre 10 e 4.000 caracteres."),Display(Name="Contexto e evidências")] public string Description{get;set;}="";
 [Required(ErrorMessage="Selecione a categoria."),Display(Name="Categoria")] public string Category{get;set;}="";
 [Required(ErrorMessage="Selecione a prioridade."),Display(Name="Prioridade")] public string Priority{get;set;}="normal";
}
public sealed record SupportTicketDetailsViewModel(Guid Id,string Subject,string Category,string Priority,string Status,IReadOnlyList<TicketMessageViewModel> Messages);
public sealed record TicketMessageViewModel(string Author,string Message,DateTimeOffset CreatedAt);
public sealed record KnowledgeArticleViewModel(Guid Id,string Title,string Summary,string Content,string Category);

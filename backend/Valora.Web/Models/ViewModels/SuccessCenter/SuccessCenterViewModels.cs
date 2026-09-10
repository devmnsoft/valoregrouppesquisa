using System.ComponentModel.DataAnnotations;
namespace Valora.Web.Models.ViewModels.SuccessCenter;
using Valora.Application.SuccessCenter;

public sealed record SuccessMetric(string Label, string Value, string Detail, string Tone = "neutral");
public sealed record SuccessCenterPageViewModel(string Eyebrow, string Title, string Description, string Section, IReadOnlyList<SuccessMetric> Metrics);
public sealed class CreateSupportTicketViewModel {
    [Required(ErrorMessage = "Informe o assunto."), StringLength(180, MinimumLength = 5, ErrorMessage = "Use entre 5 e 180 caracteres."), Display(Name = "Assunto")] public string Subject { get; set; } = "";
    [Required(ErrorMessage = "Descreva o contexto."), StringLength(4000, MinimumLength = 10, ErrorMessage = "Use entre 10 e 4.000 caracteres."), Display(Name = "Contexto e evidências")] public string Description { get; set; } = "";
    [Required(ErrorMessage = "Selecione a categoria."), Display(Name = "Categoria")] public string Category { get; set; } = "";
    [Required(ErrorMessage = "Selecione a prioridade."), Display(Name = "Prioridade")] public string Priority { get; set; } = "normal";
}
public sealed record SupportTicketListViewModel(SupportTicketPage Page, SupportTicketQuery Query, string? Success, string? Error) { public IReadOnlyList<SupportTicket> Tickets=>Page.Items; }
public sealed class TicketReplyViewModel { [Required, StringLength(4000, MinimumLength=2), Display(Name="Resposta")] public string Message { get; set; } = ""; }
public sealed record TicketTimelineViewModel(string Kind, string Author, string Text, DateTimeOffset CreatedAt);
public sealed record SupportTicketDetailsViewModel(Guid Id, string Subject, string Description, string Category, string Priority, string Status, IReadOnlyList<TicketTimelineViewModel> Timeline, TicketReplyViewModel Reply, string? Success, string? Error) {
    public static SupportTicketDetailsViewModel From(SupportTicketDetails details, string? success, string? error, TicketReplyViewModel? reply=null) {
        var messages=details.Messages.Select(x=>new TicketTimelineViewModel("message",x.Author,x.Message,x.CreatedAt));
        var events=details.Events.Select(x=>new TicketTimelineViewModel("event",x.Author,EventText(x),x.CreatedAt));
        return new(details.Ticket.Id,details.Ticket.Subject,details.Ticket.Description,details.Ticket.Category,details.Ticket.Priority,details.Ticket.Status,messages.Concat(events).OrderBy(x=>x.CreatedAt).ToArray(),reply??new(),success,error);
    }
    private static string EventText(TicketEvent x) => x.EventType switch { "support.ticket.created"=>"Chamado aberto.", "support.ticket.resolved"=>"Chamado resolvido.", "support.ticket.reopened"=>"Chamado reaberto.", "support.ticket.assigned"=>"Responsável atribuído.", _=>"Atualização registrada." };
}
public sealed record KnowledgeArticleViewModel(Guid Id, string Title, string Summary, string Content, string Category);

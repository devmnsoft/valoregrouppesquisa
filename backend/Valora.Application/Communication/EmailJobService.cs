namespace Valora.Application.Communication;
public sealed class EmailJobService { public object EnqueueResultEmailPlan() => new { queue = "valorapesquisa.email_jobs", status = "pending", allowedStatuses = new[] { "pending", "processing", "sent", "failed", "retry" } }; }

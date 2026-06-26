namespace Valora.Application.Communication;
public sealed class EmailJobService { public object EnqueueResultEmailPlan() => new { queue = "communication.email_jobs", status = "pending", allowedStatuses = new[] { "pending", "processing", "sent", "failed", "retry" } }; }

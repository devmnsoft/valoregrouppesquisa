using Microsoft.Extensions.Logging;
using Valora.Application.Security;

namespace Valora.Application.Communication;

public sealed class EmailJobService(ILogger<EmailJobService> logger)
{
    public object EnqueueResultEmailPlan()
    {
        try
        {
            logger.LogInformation("Email job enqueue plan requested. Status={Status}", "pending");
            return new { queue = "valorapesquisa.email_jobs", status = "pending", allowedStatuses = new[] { "pending", "processing", "sent", "failed", "pending-provider", "failed-config" } };
        }
        catch (Exception ex)
        {
            var last_error = LogSanitizer.SanitizeError(ex);
            logger.LogError(ex, "Email job enqueue failed. Status={Status} LastError={LastError}", "failed", last_error);
            return new { status = "failed", last_error };
        }
    }
}

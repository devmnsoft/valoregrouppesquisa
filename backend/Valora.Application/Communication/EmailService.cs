namespace Valora.Application.Communication;

public sealed class EmailService
{
    public object BuildSendPlan(string to, string subject) => new { to, subject, status = "pending", transport = "smtp" };
}

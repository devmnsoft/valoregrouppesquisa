using System.Net.Mail;

namespace Valora.Application.Communication;

public sealed record EmailConfigurationStatus(bool Ok,bool Enabled,string Provider,bool FromEmailConfigured,bool SmtpHostConfigured,bool SmtpUserConfigured,bool SmtpPasswordConfigured,bool CanSend,IReadOnlyList<string> Errors);

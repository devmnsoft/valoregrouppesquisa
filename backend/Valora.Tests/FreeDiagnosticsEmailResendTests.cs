using System;
using System.IO;
using Xunit;
public sealed class FreeDiagnosticsEmailResendTests { [Fact] public void Resend_limit_and_audit_are_declared(){ var s=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"../../../../Valora.Application/FreeDiagnostics/FreeDiagnosticsAppService.cs")); Assert.Contains(">= 3",s); Assert.Contains("free_survey.result_email_resent",s); } }

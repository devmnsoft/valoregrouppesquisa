using System;
using System.IO;
using Xunit;
public sealed class FreeDiagnosticsLgpdAuditTests { [Fact] public void Lgpd_events_are_documented(){ var s=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"../../../../../FREE_SURVEY_LGPD_AUDIT.md")); Assert.Contains("free_survey.email_sent",s); Assert.Contains("metadata sanitizada",s); } }

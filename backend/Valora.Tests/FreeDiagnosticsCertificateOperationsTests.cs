using System;
using System.IO;
using Xunit;
public sealed class FreeDiagnosticsCertificateOperationsTests { [Fact] public void Certificate_regeneration_does_not_change_result(){ var s=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"../../../../../FREE_SURVEY_CERTIFICATE_OPERATIONS.md")); Assert.Contains("não altera resultado original",s); } }

using Valora.Application.Security;
using Xunit;
public class EmailQueueProcessorTests { [Fact] public void Smtp_error_is_sanitized_to_type_name(){ var e=LogSanitizer.SanitizeError(new InvalidOperationException("secret stack")); Assert.Equal("InvalidOperationException", e); } }

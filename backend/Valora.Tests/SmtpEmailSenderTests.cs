using Valora.Application.Security;
using Xunit;
[Trait("Category", "Unit")]
public class SmtpEmailSenderTests { [Fact] public void Mask_email_does_not_return_full_recipient(){ Assert.Equal("u***@example.com", LogSanitizer.MaskEmail("user@example.com")); } }

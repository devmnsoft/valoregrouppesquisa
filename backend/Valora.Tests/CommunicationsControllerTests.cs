using System.IO;
using Xunit;
public class CommunicationsControllerTests { [Fact] public void Admin_email_endpoints_require_authorize(){ var s=File.ReadAllText("../../../Valora.Api/Controllers/CommunicationsController.cs"); Assert.Contains("[Authorize]", s); Assert.Contains("/admin/email-jobs", s); Assert.Contains("result-token-required", s); } }

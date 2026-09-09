using System.IO;
using Xunit;
using Valora.Tests.Support;
[Trait("Category", "StaticContract")]
public class CommunicationsControllerTests { [Fact] public void Admin_email_endpoints_require_authorize(){ var s=File.ReadAllText(RepositoryPaths.ApiFile("Controllers", "CommunicationsController.cs")); Assert.Contains("[Authorize]", s); Assert.Contains("/admin/email-jobs", s); Assert.Contains("result-token-required", s); } }

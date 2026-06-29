using System.IO;
using Xunit;
public class CspPolicyTests { [Fact] public void Firebase_csp_allows_bootstrap_and_api(){ var s=File.ReadAllText("../../../../firebase.json"); Assert.Contains("https://cdn.jsdelivr.net", s); Assert.Contains("https://api.valoragroup.mnsoft.com.br", s); Assert.Contains("script-src-elem", s); Assert.Contains("style-src-elem", s); } }

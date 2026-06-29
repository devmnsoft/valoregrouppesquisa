using System;
using System.IO;
using Xunit;
public sealed class FreeDiagnosticsAdminTests { [Fact] public void Admin_panel_and_api_contract_exist(){ Assert.Contains("/admin/free-diagnostics", File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"../../../../Valora.Api/Controllers/FreeDiagnosticsController.cs"))); Assert.Contains("Diagnósticos gratuitos", File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"../../../../Valora.Web/Views/FreeDiagnostics/Index.cshtml"))); } }

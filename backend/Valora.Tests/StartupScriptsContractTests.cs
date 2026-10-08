using Valora.Tests.Support;

namespace Valora.Tests;

[Trait("Category", "StaticContract")]
public sealed class StartupScriptsContractTests {
    [Fact]
    public void LocalStartupScriptsGuardCanonicalPortsAndDoNotKillForeignDotnetProcesses() {
        var batch = File.ReadAllText(RepositoryPaths.BackendFile("run-local.bat"));
        var powershell = File.ReadAllText(RepositoryPaths.BackendFile("scripts", "run-local.ps1"));
        var bash = File.ReadAllText(RepositoryPaths.BackendFile("run-local.sh"));

        Assert.Contains("scripts\\run-local.ps1", batch);
        Assert.Contains("Get-NetTCPConnection -LocalPort", powershell);
        Assert.Contains("Win32_Process", powershell);
        Assert.Contains("CommandLine", powershell);
        Assert.Contains("Assert-PortFree -Port 7088 -ServiceName \"Valora.Web\"", powershell);
        Assert.Contains("Nao foi possivel iniciar o ${ServiceName}: a porta $Port esta ocupada.", powershell);
        Assert.Contains("Start-Process -FilePath \"dotnet\"", powershell);
        Assert.Contains("-PassThru", powershell);
        Assert.Contains("Stop-ProcessTree", powershell);
        Assert.DoesNotContain("taskkill", powershell, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stop-Process -Name dotnet", powershell, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("assert_port_free 7088 \"Valora.Web\"", bash);
        Assert.Contains("port_listeners()", bash);
        Assert.Contains("Api__BaseUrl=\"$api_url\"", bash);
        Assert.Contains("--urls \"$web_http_url;$web_https_url\"", bash);
        Assert.DoesNotContain("killall dotnet", bash, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pkill dotnet", bash, StringComparison.OrdinalIgnoreCase);
    }
}

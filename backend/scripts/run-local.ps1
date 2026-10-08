$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$ApiUrl = "http://localhost:5080"
$WebHttpUrl = "http://localhost:5088"
$WebHttpsUrl = "https://localhost:7088"
$StartedProcesses = New-Object System.Collections.Generic.List[System.Diagnostics.Process]

function Get-PortListeners {
    param([int]$Port)

    Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Sort-Object LocalAddress, OwningProcess |
        ForEach-Object {
            $process = Get-CimInstance Win32_Process -Filter "ProcessId=$($_.OwningProcess)" -ErrorAction SilentlyContinue
            [pscustomobject]@{
                LocalAddress = $_.LocalAddress
                LocalPort = $_.LocalPort
                State = $_.State
                PID = $_.OwningProcess
                Name = $process.Name
                ExecutablePath = $process.ExecutablePath
                CommandLine = $process.CommandLine
            }
        }
}

function Format-PortListeners {
    param([object[]]$Listeners)

    foreach ($listener in $Listeners) {
        "  - $($listener.LocalAddress):$($listener.LocalPort) PID=$($listener.PID) Name=$($listener.Name) Path=$($listener.ExecutablePath) CommandLine=$($listener.CommandLine)"
    }
}

function Assert-PortFree {
    param([int]$Port, [string]$ServiceName)

    $listeners = @(Get-PortListeners -Port $Port)
    if ($listeners.Count -eq 0) { return }

    Write-Error @"
Nao foi possivel iniciar o ${ServiceName}: a porta $Port esta ocupada. Encerre a instancia anterior ou revise a configuracao de inicializacao.
Listeners encontrados:
$(Format-PortListeners -Listeners $listeners | Out-String)
"@
}

function Stop-ProcessTree {
    param([int]$ProcessId)

    $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$ProcessId" -ErrorAction SilentlyContinue)
    foreach ($child in $children) {
        Stop-ProcessTree -ProcessId $child.ProcessId
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Stop-StartedProcesses {
    foreach ($process in @($StartedProcesses)) {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-ProcessTree -ProcessId $process.Id
        }
    }
}

function Test-ValoraListener {
    param([int]$Port, [string]$ServiceName)

    $listeners = @(Get-PortListeners -Port $Port)
    if ($listeners.Count -eq 0) { return $false }

    foreach ($listener in $listeners) {
        $identity = "$($listener.Name) $($listener.ExecutablePath) $($listener.CommandLine)"
        if ($identity -notmatch [regex]::Escape($ServiceName)) {
            Write-Error @"
A porta $Port respondeu, mas nao foi possivel confirmar que pertence a $ServiceName.
Listeners encontrados:
$(Format-PortListeners -Listeners $listeners | Out-String)
"@
        }
    }

    return $true
}

function Wait-HttpReady {
    param([string]$Url, [int]$Port, [string]$ServiceName)

    Write-Host "Aguardando $ServiceName em $Url/health" -NoNewline
    for ($attempt = 1; $attempt -le 60; $attempt++) {
        foreach ($process in @($StartedProcesses)) {
            if ($process.HasExited) {
                throw "$($process.ProcessName) encerrou antes de $ServiceName ficar pronto. ExitCode=$($process.ExitCode)"
            }
        }

        try {
            $response = Invoke-WebRequest -UseBasicParsing -TimeoutSec 2 "$Url/health"
            if ($response.StatusCode -eq 200 -and (Test-ValoraListener -Port $Port -ServiceName $ServiceName)) {
                Write-Host " pronta."
                return
            }
        }
        catch {
            if ($attempt -eq 60) { throw }
        }

        Write-Host "." -NoNewline
        Start-Sleep -Seconds 1
    }

    throw "Tempo esgotado aguardando $ServiceName em $Url/health."
}

try {
    Assert-PortFree -Port 5080 -ServiceName "Valora.Api"
    Assert-PortFree -Port 5088 -ServiceName "Valora.Web"
    Assert-PortFree -Port 7088 -ServiceName "Valora.Web"

    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $api = Start-Process -FilePath "dotnet" -WorkingDirectory $Root -WindowStyle Hidden -PassThru -ArgumentList @(
        "run", "--no-launch-profile", "--project", "Valora.Api\Valora.Api.csproj", "--urls", $ApiUrl
    )
    $StartedProcesses.Add($api)

    Wait-HttpReady -Url $ApiUrl -Port 5080 -ServiceName "Valora.Api"

    $env:Api__BaseUrl = $ApiUrl
    $web = Start-Process -FilePath "dotnet" -WorkingDirectory $Root -WindowStyle Hidden -PassThru -ArgumentList @(
        "run", "--no-launch-profile", "--project", "Valora.Web\Valora.Web.csproj", "--urls", "$WebHttpUrl;$WebHttpsUrl"
    )
    $StartedProcesses.Add($web)

    Write-Host "Aguardando Valora.Web em $WebHttpUrl e $WebHttpsUrl" -NoNewline
    for ($attempt = 1; $attempt -le 60; $attempt++) {
        foreach ($process in @($StartedProcesses)) {
            if ($process.HasExited) {
                throw "$($process.ProcessName) encerrou antes de Valora.Web ficar pronto. ExitCode=$($process.ExitCode)"
            }
        }

        if ((Test-ValoraListener -Port 5088 -ServiceName "Valora.Web") -and (Test-ValoraListener -Port 7088 -ServiceName "Valora.Web")) {
            Write-Host " pronta."
            break
        }

        if ($attempt -eq 60) {
            throw "Tempo esgotado aguardando Valora.Web nas portas 5088 e 7088."
        }

        Write-Host "." -NoNewline
        Start-Sleep -Seconds 1
    }

    Write-Host "Valora.Api: $ApiUrl | Valora.Web: $WebHttpUrl e $WebHttpsUrl"
    Write-Host "Pressione Ctrl+C para encerrar somente os processos iniciados por este script."

    while ($true) {
        foreach ($process in @($StartedProcesses)) {
            if ($process.HasExited) {
                throw "$($process.ProcessName) encerrou. Encerrando os demais processos iniciados pelo script."
            }
        }
        Start-Sleep -Seconds 1
    }
}
finally {
    Stop-StartedProcesses
}

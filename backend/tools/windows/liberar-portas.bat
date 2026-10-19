@echo off
setlocal
echo ============================================================
echo   Liberando portas locais do Valora Insight (5080, 5088, 7088)
echo ============================================================
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ports = @(5080, 5088, 7088); ^
   foreach ($p in $ports) { ^
     $conns = Get-NetTCPConnection -LocalPort $p -State Listen -ErrorAction SilentlyContinue; ^
     foreach ($c in $conns) { ^
       $proc = Get-Process -Id $c.OwningProcess -ErrorAction SilentlyContinue; ^
       if ($proc) { ^
         Write-Host ('Encerrando processo PID={0} ({1}) na porta {2}...' -f $proc.Id, $proc.ProcessName, $p) -ForegroundColor Yellow; ^
         Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue; ^
       } ^
     } ^
   }; ^
   Start-Sleep -Seconds 1; ^
   Write-Host 'Portas verificadas com sucesso.' -ForegroundColor Green"
exit /b 0

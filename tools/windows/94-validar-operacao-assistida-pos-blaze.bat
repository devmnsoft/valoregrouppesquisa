@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/40] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [2/40] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [3/40] Config Blaze...
call npm run config:blaze
if errorlevel 1 goto erro

echo [4/40] Config live...
call npm run prod:config-live
if errorlevel 1 goto erro

echo [5/40] Painel operacao assistida...
call npm run ops:assisted-panel
if errorlevel 1 goto erro

echo [6/40] Guardrails custo Blaze...
call npm run blaze:cost-guardrails
if errorlevel 1 goto erro

echo [7/40] Fila email resiliente...
call npm run email:queue-resilience
if errorlevel 1 goto erro

echo [8/40] Retry agendado email...
call npm run email:scheduled-retry
if errorlevel 1 goto erro

echo [9/40] Logs sanitizados...
call npm run ops:log-sanitization
if errorlevel 1 goto erro

echo [10/40] Reparo pesquisa gratis...
call npm run ops:free-survey-repair
if errorlevel 1 goto erro

echo [11/40] Sem regressao menu dashboard only...
call npm run admin:no-dashboard-only
if errorlevel 1 goto erro

echo [12/40] Menu mobile lista completa...
call npm run admin:mobile-menu-full-list
if errorlevel 1 goto erro

echo [13/40] Sem regressao expiracao pesquisa gratis...
call npm run home:no-expiration-regression
if errorlevel 1 goto erro

echo [14/40] Backup rollback...
call npm run prod:backup-rollback
if errorlevel 1 goto erro

echo [15/40] Relatorio diario operacao...
call npm run ops:daily-report
if errorlevel 1 goto erro

echo [16/40] Paridade operacao Valora.Web...
call npm run web:operation-parity
if errorlevel 1 goto erro

echo [17/40] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [18/40] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [19/40] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [20/40] Documento auditoria...
if not exist SPRINT_60_ASSISTED_OPERATION_AUDIT.md goto erro

echo OPERACAO ASSISTIDA POS-BLAZE VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DA OPERACAO ASSISTIDA POS-BLAZE.
pause
exit /b 1

@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/40] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [2/40] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [3/40] Credenciais Firebase Admin...
call npm run firebase:admin-credentials
if errorlevel 1 goto erro

echo [4/40] Segurança dos repairs...
call npm run firebase:repair-safety
if errorlevel 1 goto erro

echo [5/40] Dry-run repair link gratuito...
call npm run home:repair-free-survey-link -- --dry-run --backup --project gestordepesquisa
if errorlevel 1 goto erro

echo [6/40] Expiração pesquisa grátis...
call npm run home:free-survey-expiration
if errorlevel 1 goto erro

echo [7/40] Link público...
call npm run home:validate-public-link
if errorlevel 1 goto erro

echo [8/40] Link gratuito...
call npm run home:free-survey-link
if errorlevel 1 goto erro

echo [9/40] Paridade correções index antigo...
call npm run legacy:corrections-parity
if errorlevel 1 goto erro

echo [10/40] Contrato runtime cliente...
call npm run client:runtime-contract
if errorlevel 1 goto erro

echo [11/40] Contrato runtime usuário...
call npm run user:runtime-contract
if errorlevel 1 goto erro

echo [12/40] Erros amigáveis Auth...
call npm run auth:friendly-errors
if errorlevel 1 goto erro

echo [13/40] Menu admin mobile contrato...
call npm run admin:mobile-menu-runtime-contract
if errorlevel 1 goto erro

echo [14/40] Menu admin mobile runtime...
call npm run admin:mobile-menu-runtime
if errorlevel 1 goto erro

echo [15/40] Paridade Valora.Web...
call npm run web:correction-parity
if errorlevel 1 goto erro

echo [16/40] Menu Valora.Web runtime...
call npm run web:mobile-menu-runtime
if errorlevel 1 goto erro

echo [17/40] Sem resposta demonstrativa...
call npm run email:no-demo-response
if errorlevel 1 goto erro

echo [18/40] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [19/40] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [20/40] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [21/40] Documento auditoria...
if not exist SPRINT_53_LEGACY_INDEX_FULL_FIX_AUDIT.md goto erro

echo CORRECOES DO INDEX ANTIGO E VALORA.WEB VALIDADAS.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DAS CORRECOES DO INDEX ANTIGO E VALORA.WEB.
pause
exit /b 1

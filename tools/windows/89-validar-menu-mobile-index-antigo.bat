@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/35] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [2/35] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [3/35] Expiracao pesquisa gratis...
call npm run home:free-survey-expiration
if errorlevel 1 goto erro

echo [4/35] Link publico...
call npm run home:validate-public-link
if errorlevel 1 goto erro

echo [5/35] Link gratuito...
call npm run home:free-survey-link
if errorlevel 1 goto erro

echo [6/35] Email real response flow legado...
call npm run legacy:email-real-response-flow
if errorlevel 1 goto erro

echo [7/35] Contrato runtime cliente...
call npm run client:runtime-contract
if errorlevel 1 goto erro

echo [8/35] Contrato runtime usuario...
call npm run user:runtime-contract
if errorlevel 1 goto erro

echo [9/35] Menu admin mobile contrato...
call npm run admin:mobile-menu-runtime-contract
if errorlevel 1 goto erro

echo [10/35] Menu admin mobile lista completa...
call npm run admin:mobile-menu-full-list
if errorlevel 1 goto erro

echo [11/35] Menu admin mobile runtime...
call npm run admin:mobile-menu-runtime
if errorlevel 1 goto erro

echo [12/35] Paridade Valora.Web...
call npm run web:correction-parity
if errorlevel 1 goto erro

echo [13/35] Menu Valora.Web runtime...
call npm run web:mobile-menu-runtime
if errorlevel 1 goto erro

echo [14/35] Sem resposta demonstrativa...
call npm run email:no-demo-response
if errorlevel 1 goto erro

echo [15/35] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [16/35] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [17/35] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [18/35] Documento auditoria...
if not exist SPRINT_55_LEGACY_MOBILE_MENU_FINAL_AUDIT.md goto erro

echo MENU MOBILE DO INDEX ANTIGO VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DO MENU MOBILE DO INDEX ANTIGO.
pause
exit /b 1

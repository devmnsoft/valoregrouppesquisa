@echo off
cd /d C:\MNSOFT\valoregrouppesquisa
echo [1/35] Build backend...
call npm run backend:build
if errorlevel 1 goto erro
echo [2/35] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro
echo [3/35] Validar credenciais Firebase Admin...
call npm run firebase:admin-credentials
if errorlevel 1 goto erro
echo [4/35] Segurança scripts repair...
call npm run firebase:repair-safety
if errorlevel 1 goto erro
echo [5/35] Dry-run repair pesquisa grátis...
call npm run home:repair-free-survey-link -- --dry-run --backup --project gestordepesquisa
if errorlevel 1 goto erro
echo [6/35] Expiração robusta pesquisa grátis...
call npm run home:free-survey-expiration
if errorlevel 1 goto erro
echo [7/35] Validar link público estrutural...
call npm run home:validate-public-link
if errorlevel 1 goto erro
echo [8/35] Link pesquisa grátis...
call npm run home:free-survey-link
if errorlevel 1 goto erro
echo [9/35] Sem resposta demonstrativa...
call npm run email:no-demo-response
if errorlevel 1 goto erro
echo [10/35] Menu admin mobile estrutural...
call npm run admin:mobile-menu
if errorlevel 1 goto erro
echo [11/35] Menu admin mobile runtime...
call npm run admin:mobile-menu-runtime
if errorlevel 1 goto erro
echo [12/35] Menu Valora.Web mobile estrutural...
call npm run web:mobile-admin-menu
if errorlevel 1 goto erro
echo [13/35] Menu Valora.Web mobile runtime...
call npm run web:mobile-menu-runtime
if errorlevel 1 goto erro
echo [14/35] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro
echo [15/35] Web build...
call npm run web:build
if errorlevel 1 goto erro
echo [16/35] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro
echo [17/35] Documento auditoria...
if not exist SPRINT_52_FIREBASE_CREDENTIALS_LINK_MOBILE_AUDIT.md goto erro
echo CREDENCIAIS + LINK + EXPIRACAO + MENU RUNTIME VALIDADO.
pause
exit /b 0
:erro
echo FALHA NA VALIDACAO DE CREDENCIAIS/LINK/EXPIRACAO/MENU.
pause
exit /b 1

@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/35] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [2/35] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [3/35] Reparar link pesquisa gratis...
call npm run home:repair-free-survey-link
if errorlevel 1 goto erro

echo [4/35] Validar link publico...
call npm run home:validate-public-link
if errorlevel 1 goto erro

echo [5/35] Link pesquisa gratis...
call npm run home:free-survey-link
if errorlevel 1 goto erro

echo [6/35] Cadastro cliente...
call npm run client:create-flow
if errorlevel 1 goto erro

echo [7/35] Cadastro usuario Auth+perfil...
call npm run user:create-auth-profile-flow
if errorlevel 1 goto erro

echo [8/35] Erros Firebase Auth...
call npm run auth:firebase-errors
if errorlevel 1 goto erro

echo [9/35] Reparo perfil usuario...
call npm run user:profile-repair
if errorlevel 1 goto erro

echo [10/35] Menu admin mobile legado...
call npm run admin:mobile-menu
if errorlevel 1 goto erro

echo [11/35] Menu admin mobile Valora.Web...
call npm run web:mobile-admin-menu
if errorlevel 1 goto erro

echo [12/35] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [13/35] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [14/35] CSP local...
call npm run security:csp
if errorlevel 1 goto erro

echo [15/35] Sem resposta demonstrativa em producao...
call npm run email:no-demo-response
if errorlevel 1 goto erro

echo [16/35] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [17/35] Documento auditoria...
if not exist SPRINT_50_LINK_AUTH_MOBILE_AUDIT.md goto erro

echo LINK PUBLICO + AUTH + MENU MOBILE VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DE LINK PUBLICO/AUTH/MENU MOBILE.
pause
exit /b 1

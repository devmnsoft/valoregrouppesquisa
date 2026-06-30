@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/25] Validar bridge estrutural...
call npm run admin:mobile-bridge
if errorlevel 1 goto erro

echo [2/25] Validar cache busting bridge...
call npm run admin:mobile-bridge-cache
if errorlevel 1 goto erro

echo [3/25] Runtime bridge...
call npm run admin:mobile-bridge-runtime
if errorlevel 1 goto erro

echo [4/25] Standalone bridge sem app.js...
call npm run admin:mobile-bridge-standalone
if errorlevel 1 goto erro

echo [5/25] Runtime clique existente...
call npm run admin:mobile-click-runtime
if errorlevel 1 goto erro

echo [6/25] Lista completa menu mobile...
call npm run admin:mobile-menu-full-list
if errorlevel 1 goto erro

echo [7/25] Sem regressao Dashboard only...
call npm run admin:no-dashboard-only
if errorlevel 1 goto erro

echo [8/25] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [9/25] Build Web...
call npm run web:build
if errorlevel 1 goto erro

echo [10/25] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [11/25] Documento auditoria...
if not exist SPRINT_62_LEGACY_MOBILE_MENU_BRIDGE_AUDIT.md goto erro

echo BRIDGE DO MENU MOBILE LEGADO VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DA BRIDGE DO MENU MOBILE LEGADO.
pause
exit /b 1

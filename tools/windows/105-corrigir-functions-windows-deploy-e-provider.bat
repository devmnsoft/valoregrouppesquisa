@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/18] Segurança sem segredos...
call npm run security:no-secrets
if errorlevel 1 goto erro

echo [2/18] Scripts obrigatórios...
call npm run scripts:required
if errorlevel 1 goto erro

echo [3/18] Instalar Functions...
call npm run functions:install
if errorlevel 1 goto erro

echo [4/18] Node 22 readiness...
call npm run functions:node22-readiness
if errorlevel 1 goto erro

echo [5/18] Lint Functions Windows-safe...
call npm run functions:lint
if errorlevel 1 goto erro

echo [6/18] Provider unavailable hotfix...
call npm run legacy:provider-final-fix
if errorlevel 1 goto erro

echo [7/18] Submit público legado...
call npm run legacy:public-submit-flow
if errorlevel 1 goto erro

echo [8/18] Functions submit público...
call npm run functions:public-submit
if errorlevel 1 goto erro

echo [9/18] E-mail resultado...
call npm run legacy:result-email-send
if errorlevel 1 goto erro

echo [10/18] Planos...
call npm run legacy:plans-tab
if errorlevel 1 goto erro

echo [11/18] Token gratuito...
call npm run legacy:free-token-never-expires
if errorlevel 1 goto erro

echo [12/18] Certificado...
call npm run legacy:certificate-complete
if errorlevel 1 goto erro

echo [13/18] Menu mobile...
call npm run legacy:admin-mobile-functional
if errorlevel 1 goto erro

echo [14/18] Build produção...
call npm run build:prod
if errorlevel 1 goto erro

echo [15/18] Validar dist...
call npm run hosting:dist-build
if errorlevel 1 goto erro

echo [16/18] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [17/18] Auditoria...
if not exist SPRINT_71_FUNCTIONS_WINDOWS_PROVIDER_FIX_AUDIT.md goto erro

echo Correção validada.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO.
pause
exit /b 1

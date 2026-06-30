@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/20] Segurança sem segredos...
call npm run security:no-secrets
if errorlevel 1 goto erro

echo [2/20] Scripts obrigatórios...
call npm run scripts:required
if errorlevel 1 goto erro

echo [3/20] Hotfix provider unavailable...
call npm run legacy:provider-hotfix
if errorlevel 1 goto erro

echo [4/20] Submit público legado...
call npm run legacy:public-submit-flow
if errorlevel 1 goto erro

echo [5/20] Functions submit público...
call npm run functions:public-submit
if errorlevel 1 goto erro

echo [6/20] E-mail resultado...
call npm run legacy:result-email-send
if errorlevel 1 goto erro

echo [7/20] Planos...
call npm run legacy:plans-tab
if errorlevel 1 goto erro

echo [8/20] Certificado...
call npm run legacy:certificate-complete
if errorlevel 1 goto erro

echo [9/20] Menu mobile...
call npm run legacy:admin-mobile-functional
if errorlevel 1 goto erro

echo [10/20] Build produção dist...
call npm run build:prod
if errorlevel 1 goto erro

echo [11/20] Validar dist...
call npm run hosting:dist-build
if errorlevel 1 goto erro

echo [12/20] E2E provider hotfix...
call npm run e2e:legacy-provider-hotfix
if errorlevel 1 goto erro

echo [13/20] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [14/20] Auditoria...
if not exist SPRINT_68_PROVIDER_UNAVAILABLE_HOTFIX_AUDIT.md goto erro

echo HOTFIX PROVIDER_UNAVAILABLE VALIDADO.
pause
exit /b 0

:erro
echo FALHA NO HOTFIX PROVIDER_UNAVAILABLE.
pause
exit /b 1

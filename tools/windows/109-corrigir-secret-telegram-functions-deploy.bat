@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/18] Segurança sem segredos...
call npm run security:no-secrets
if errorlevel 1 goto erro

echo [2/18] Instalar dependencias Functions...
call npm run functions:install
if errorlevel 1 goto erro

echo [3/18] Node 22 readiness...
call npm run functions:node22-readiness
if errorlevel 1 goto erro

echo [4/18] Secrets readiness...
call npm run functions:secrets-readiness
if errorlevel 1 goto erro

echo [5/18] Lint Functions...
call npm run functions:lint
if errorlevel 1 goto erro

echo [6/18] Build produção...
call npm run build:prod
if errorlevel 1 goto erro

echo [7/18] Validar dist...
call npm run hosting:dist-build
if errorlevel 1 goto erro

echo [8/18] Cliente legado...
call npm run legacy:client-create-flow
if errorlevel 1 goto erro

echo [9/18] Usuário legado...
call npm run legacy:user-create-flow
if errorlevel 1 goto erro

echo [10/18] Link demo bloqueado...
call npm run legacy:demo-link-blocked
if errorlevel 1 goto erro

echo [11/18] Link oficial gratuito...
call npm run legacy:official-free-link
if errorlevel 1 goto erro

echo [12/18] Provider hotfix...
call npm run legacy:provider-demo-hotfix
if errorlevel 1 goto erro

echo [13/18] E-mail resultado...
call npm run legacy:result-email-send
if errorlevel 1 goto erro

echo [14/18] Certificado...
call npm run legacy:certificate-complete
if errorlevel 1 goto erro

echo [15/18] Planos...
call npm run legacy:plans-tab
if errorlevel 1 goto erro

echo [16/18] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [17/18] Auditoria...
if not exist SPRINT_75_FUNCTIONS_TELEGRAM_SECRET_OPTIONAL_AUDIT.md goto erro

echo VALIDADO: TELEGRAM OPCIONAL + FUNCTIONS DEPLOY READY.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO.
pause
exit /b 1

@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/20] Segurança sem segredos...
call npm run security:no-secrets
if errorlevel 1 goto erro

echo [2/20] Bloqueio link demo...
call npm run legacy:demo-link-blocked
if errorlevel 1 goto erro

echo [3/20] Link oficial gratuito...
call npm run legacy:official-free-link
if errorlevel 1 goto erro

echo [4/20] Hotfix provider demo...
call npm run legacy:provider-demo-hotfix
if errorlevel 1 goto erro

echo [5/20] Submit público...
call npm run legacy:public-submit-flow
if errorlevel 1 goto erro

echo [6/20] Functions submit...
call npm run functions:public-submit
if errorlevel 1 goto erro

echo [7/20] E-mail...
call npm run legacy:result-email-send
if errorlevel 1 goto erro

echo [8/20] Certificado...
call npm run legacy:certificate-complete
if errorlevel 1 goto erro

echo [9/20] Planos...
call npm run legacy:plans-tab
if errorlevel 1 goto erro

echo [10/20] Menu mobile...
call npm run legacy:admin-mobile-functional
if errorlevel 1 goto erro

echo [11/20] Build produção...
call npm run build:prod
if errorlevel 1 goto erro

echo [12/20] Validar dist...
call npm run hosting:dist-build
if errorlevel 1 goto erro

echo [13/20] E2E demo link...
call npm run e2e:legacy-demo-link
if errorlevel 1 goto erro

echo [14/20] E2E submit oficial...
call npm run e2e:legacy-official-submit
if errorlevel 1 goto erro

echo [15/20] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [16/20] Auditoria...
if not exist SPRINT_73_DEMO_LINK_OFFICIAL_SURVEY_FIX_AUDIT.md goto erro

echo LINK DEMO + PESQUISA OFICIAL CORRIGIDOS.
pause
exit /b 0

:erro
echo FALHA NA CORRECAO LINK DEMO + PESQUISA OFICIAL.
pause
exit /b 1

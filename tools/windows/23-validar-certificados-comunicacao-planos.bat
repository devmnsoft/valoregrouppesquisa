@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/7] Validando codigo...
call npm run check
if errorlevel 1 goto erro

echo [2/7] Validando certificados...
node scripts\validate-certificates.js
if errorlevel 1 goto erro

echo [3/7] Validando comunicacao...
node scripts\validate-communication-flow.js
if errorlevel 1 goto erro

echo [4/7] Auditando planos...
node scripts\audit-plan-capabilities.js
if errorlevel 1 goto erro

echo [5/7] Validando contratos dos planos...
node scripts\validate-plan-contracts.js
if errorlevel 1 goto erro

echo [6/7] Build producao...
call npm run build:prod
if errorlevel 1 goto erro

echo [7/7] Health check local do dist...
node scripts\validate-release-candidate.js
if errorlevel 1 goto erro

echo VALIDACAO CONCLUIDA.
pause
exit /b 0

:erro
echo VALIDACAO FALHOU.
pause
exit /b 1

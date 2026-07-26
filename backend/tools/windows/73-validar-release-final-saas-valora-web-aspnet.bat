@echo off
cd /d C:\DBBACK\valoregrouppesquisa
echo [1/90] Subindo ambiente local live...
call npm run local:live:up
if errorlevel 1 goto erro
echo [2/90] Aplicando migrations...
call npm run local:migrations:apply
if errorlevel 1 goto erro
echo [3/90] Valora.Web ASP.NET only...
call npm run web:aspnet-only
if errorlevel 1 goto erro
echo [4/90] Build API...
call npm run backend:build
if errorlevel 1 goto erro
echo [5/90] Testes API...
call npm run backend:test
if errorlevel 1 goto erro
echo [6/90] Build Valora.Web...
call npm run web:build
if errorlevel 1 goto erro
echo [7/90] Projeto Valora.Web...
call npm run web:validate
if errorlevel 1 goto erro
echo [8/90] Sem acesso direto a dados...
call npm run web:no-data-access
if errorlevel 1 goto erro
echo [9/90] Sem binarios...
call npm run web:no-binaries
if errorlevel 1 goto erro
echo [10/90] Sem placeholders...
call npm run web:no-placeholders
if errorlevel 1 goto erro
echo [11/90] Sem render oficial generico...
call npm run web:no-generic-official-render
if errorlevel 1 goto erro
echo [12/90] Renderizadores especificos...
call npm run web:specific-renderers
if errorlevel 1 goto erro
echo [41/90] Release final Web...
call npm run web:final-release-gate
if errorlevel 1 goto erro
echo [90/90] Release final SaaS validado.
exit /b 0
:erro
echo FALHA NA VALIDACAO FINAL DO SAAS VALORA.WEB ASP.NET.
call npm run local:live:down
exit /b 1

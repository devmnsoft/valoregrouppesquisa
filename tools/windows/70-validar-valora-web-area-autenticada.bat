@echo off
cd /d C:\DBBACK\valoregrouppesquisa

echo [1/60] Subindo ambiente local live...
call npm run local:live:up
if errorlevel 1 goto erro

echo [2/60] Aplicando migrations...
call npm run local:migrations:apply
if errorlevel 1 goto erro

echo [3/60] Build API...
call npm run backend:build
if errorlevel 1 goto erro

echo [4/60] Testes API...
call npm run backend:test
if errorlevel 1 goto erro

echo [5/60] Build Valora.Web...
call npm run web:build
if errorlevel 1 goto erro

echo [6/60] Projeto Valora.Web...
call npm run web:validate
if errorlevel 1 goto erro

echo [7/60] Sem binarios...
call npm run web:no-binaries
if errorlevel 1 goto erro

echo [8/60] Sem placeholders...
call npm run web:no-placeholders
if errorlevel 1 goto erro

echo [9/60] Formularios reais...
call npm run web:real-forms
if errorlevel 1 goto erro

echo [10/60] Uso real da API...
call npm run web:real-api-usage
if errorlevel 1 goto erro

echo [11/60] Area autenticada...
call npm run web:authenticated-modules
if errorlevel 1 goto erro

echo [12/60] API gaps CRUD...
call npm run web:crud-api-gaps
if errorlevel 1 goto erro

echo [13/60] Auth guards...
call npm run web:auth-guards
if errorlevel 1 goto erro

echo [14/60] Sem JSON bruto...
call npm run web:no-json-dump
if errorlevel 1 goto erro

echo [15/60] UX Admin...
call npm run web:admin-ux
if errorlevel 1 goto erro

echo [16/60] Endpoints API para Web...
call npm run web:api-endpoints
if errorlevel 1 goto erro

echo [60/60] Area autenticada validada.
echo VALORA.WEB AREA AUTENTICADA SAAS VALIDADA.
pause
exit /b 0
:erro
echo FALHA NA VALIDACAO DA AREA AUTENTICADA VALORA.WEB.
call npm run local:live:down
pause
exit /b 1

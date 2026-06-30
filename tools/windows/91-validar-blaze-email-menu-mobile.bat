@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/35] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [2/35] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [3/35] Config Blaze...
call npm run config:blaze
if errorlevel 1 goto erro

echo [4/35] Readiness Functions SMTP...
call npm run email:blaze-functions-readiness
if errorlevel 1 goto erro

echo [5/35] Provider auto submissao...
call npm run home:submission-auto-provider
if errorlevel 1 goto erro

echo [6/35] Pesquisa gratis nunca expira indevidamente...
call npm run home:free-survey-never-expires
if errorlevel 1 goto erro

echo [7/35] Email apos submit...
call npm run home:email-after-submit
if errorlevel 1 goto erro

echo [8/35] Sem resposta demonstrativa...
call npm run email:no-demo-response
if errorlevel 1 goto erro

echo [9/35] Menu mobile lista completa...
call npm run admin:mobile-menu-full-list
if errorlevel 1 goto erro

echo [10/35] Menu mobile runtime...
call npm run admin:mobile-menu-runtime
if errorlevel 1 goto erro

echo [11/35] Diagnostico ambiente legado...
call npm run legacy:environment-diagnostics
if errorlevel 1 goto erro

echo [12/35] Paridade Valora.Web...
call npm run web:correction-parity
if errorlevel 1 goto erro

echo [13/35] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [14/35] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [15/35] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [16/35] Documento auditoria...
if not exist SPRINT_57_BLAZE_EMAIL_MOBILE_FINAL_AUDIT.md goto erro

echo BLAZE + EMAIL + MENU MOBILE VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO BLAZE + EMAIL + MENU MOBILE.
pause
exit /b 1

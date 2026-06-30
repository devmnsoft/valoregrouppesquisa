@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/35] Segurança sem segredos...
call npm run security:no-secrets
if errorlevel 1 goto erro

echo [2/35] Validar scriptbd completo...
call npm run db:scriptbd-completo
if errorlevel 1 goto erro

echo [3/35] Validar bootstrap PostgreSQL local...
call npm run db:local-bootstrap
if errorlevel 1 goto erro

echo [4/35] Paridade API x legado...
call npm run migration:api-parity
if errorlevel 1 goto erro

echo [5/35] Paridade Web x legado...
call npm run migration:web-parity
if errorlevel 1 goto erro

echo [6/35] Paridade certificado...
call npm run migration:certificate-parity
if errorlevel 1 goto erro

echo [7/35] Paridade e-mail...
call npm run migration:email-parity
if errorlevel 1 goto erro

echo [8/35] Menu mobile legado...
call npm run admin:mobile-final
if errorlevel 1 goto erro

echo [9/35] Menu mobile Valora.Web...
call npm run web:mobile-menu-parity
if errorlevel 1 goto erro

echo [10/35] Token gratuito sem expirar...
call npm run home:free-token-runtime
if errorlevel 1 goto erro

echo [11/35] Fluxo e-mail legado...
call npm run legacy:result-email-flow
if errorlevel 1 goto erro

echo [12/35] Certificado legado...
call npm run legacy:certificate-flow
if errorlevel 1 goto erro

echo [13/35] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [14/35] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [15/35] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [16/35] Build Web...
call npm run web:build
if errorlevel 1 goto erro

echo [17/35] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [18/35] Documento auditoria...
if not exist SPRINT_64_MIGRATION_FULL_PARITY_AUDIT.md goto erro

echo MIGRACAO LEGADO PARA WEB/API VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DA MIGRACAO LEGADO PARA WEB/API.
pause
exit /b 1

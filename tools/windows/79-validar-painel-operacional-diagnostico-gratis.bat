@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/45] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [2/45] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [3/45] Painel admin diagnosticos gratuitos...
call npm run free-diagnostics:admin-panel
if errorlevel 1 goto erro

echo [4/45] API diagnosticos gratuitos...
call npm run free-diagnostics:api
if errorlevel 1 goto erro

echo [5/45] Reenvio email...
call npm run free-diagnostics:email-resend
if errorlevel 1 goto erro

echo [6/45] Auditoria LGPD...
call npm run free-diagnostics:lgpd-audit
if errorlevel 1 goto erro

echo [7/45] Dados sensiveis...
call npm run free-diagnostics:sensitive-data
if errorlevel 1 goto erro

echo [8/45] Certificado operacional...
call npm run free-diagnostics:certificate-operations
if errorlevel 1 goto erro

echo [9/45] Auditoria WhatsApp...
call npm run free-diagnostics:whatsapp-audit
if errorlevel 1 goto erro

echo [10/45] Home sem planos...
call npm run home:no-plan-cards
if errorlevel 1 goto erro

echo [11/45] Link pesquisa gratis...
call npm run home:free-survey-link
if errorlevel 1 goto erro

echo [12/45] Resultado por email obrigatorio...
call npm run home:result-email-required
if errorlevel 1 goto erro

echo [13/45] CTA WhatsApp...
call npm run home:whatsapp-cta
if errorlevel 1 goto erro

echo [14/45] SMTP real...
call npm run email:real-sender
if errorlevel 1 goto erro

echo [15/45] Sem provider placeholder...
call npm run email:no-placeholder-provider
if errorlevel 1 goto erro

echo [16/45] Certificado rico...
call npm run certificate:rich-content
if errorlevel 1 goto erro

echo [17/45] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [18/45] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [19/45] Web E2E...
call npm run web:e2e
if errorlevel 1 goto erro

echo [20/45] SaaS E2E...
call npm run prod:saas-e2e
if errorlevel 1 goto erro

echo [21/45] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [22/45] Documento auditoria sprint...
if not exist SPRINT_47_FREE_SURVEY_OPERATION_AUDIT.md goto erro

echo [23/45] Documento LGPD...
if not exist FREE_SURVEY_LGPD_AUDIT.md goto erro

echo [24/45] Documento operação...
if not exist FREE_DIAGNOSTICS_OPERATION_PANEL.md goto erro

echo PAINEL OPERACIONAL DO DIAGNOSTICO GRATIS VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DO PAINEL OPERACIONAL DO DIAGNOSTICO GRATIS.
pause
exit /b 1

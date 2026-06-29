@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/50] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [2/50] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [3/50] Seguranca pesquisa gratis...
call npm run free-survey:security
if errorlevel 1 goto erro

echo [4/50] Validacao publica certificado...
call npm run certificate:public-validation
if errorlevel 1 goto erro

echo [5/50] Entregabilidade email...
call npm run email:deliverability
if errorlevel 1 goto erro

echo [6/50] Painel operacional...
call npm run operations:panel
if errorlevel 1 goto erro

echo [7/50] API operacional...
call npm run operations:api
if errorlevel 1 goto erro

echo [8/50] Bug bash checklist...
call npm run prod:bug-bash-checklist
if errorlevel 1 goto erro

echo [9/50] Evidencia release final...
call npm run prod:final-evidence
if errorlevel 1 goto erro

echo [10/50] Home sem planos...
call npm run home:no-plan-cards
if errorlevel 1 goto erro

echo [11/50] Link pesquisa gratis...
call npm run home:free-survey-link
if errorlevel 1 goto erro

echo [12/50] Resultado por email obrigatorio...
call npm run home:result-email-required
if errorlevel 1 goto erro

echo [13/50] CTA WhatsApp...
call npm run home:whatsapp-cta
if errorlevel 1 goto erro

echo [14/50] SMTP real...
call npm run email:real-sender
if errorlevel 1 goto erro

echo [15/50] Sem provider placeholder...
call npm run email:no-placeholder-provider
if errorlevel 1 goto erro

echo [16/50] Certificado rico...
call npm run certificate:rich-content
if errorlevel 1 goto erro

echo [17/50] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [18/50] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [19/50] Web E2E...
call npm run web:e2e
if errorlevel 1 goto erro

echo [20/50] SaaS E2E...
call npm run prod:saas-e2e
if errorlevel 1 goto erro

echo [21/50] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [22/50] Documento auditoria sprint...
if not exist SPRINT_48_FINAL_HOMOLOGATION_AUDIT.md goto erro

echo [23/50] Documento bug bash...
if not exist BUG_BASH_FINAL_SAAS.md goto erro

echo [24/50] Documento evidencias...
if not exist FINAL_RELEASE_EVIDENCE.md goto erro

echo [25/50] Documento entregabilidade...
if not exist EMAIL_DELIVERABILITY_GUIDE.md goto erro

echo HOMOLOGACAO FINAL SAAS VALIDADA.
pause
exit /b 0

:erro
echo FALHA NA HOMOLOGACAO FINAL DO SAAS.
pause
exit /b 1

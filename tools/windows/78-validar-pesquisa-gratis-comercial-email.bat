@echo off
cd /d C:\MNSOFT\valoregrouppesquisa

echo [1/40] Build backend...
call npm run backend:build
if errorlevel 1 goto erro

echo [2/40] Testes backend...
call npm run backend:test
if errorlevel 1 goto erro

echo [3/40] Home sem cards de planos...
call npm run home:no-plan-cards
if errorlevel 1 goto erro

echo [4/40] Link compartilhavel da pesquisa gratis...
call npm run home:free-survey-link
if errorlevel 1 goto erro

echo [5/40] CTA WhatsApp...
call npm run home:whatsapp-cta
if errorlevel 1 goto erro

echo [6/40] Resultado por email obrigatorio...
call npm run home:result-email-required
if errorlevel 1 goto erro

echo [7/40] Contrato E2E pesquisa gratis...
call npm run home:free-survey-e2e-contract
if errorlevel 1 goto erro

echo [8/40] CSP...
call npm run security:csp
if errorlevel 1 goto erro

echo [9/40] SMTP config...
call npm run email:smtp-config
if errorlevel 1 goto erro

echo [10/40] SMTP real sender...
call npm run email:real-sender
if errorlevel 1 goto erro

echo [11/40] Email jobs schema...
call npm run email:jobs-schema
if errorlevel 1 goto erro

echo [12/40] Sem provider placeholder...
call npm run email:no-placeholder-provider
if errorlevel 1 goto erro

echo [13/40] Segurança endpoints email...
call npm run email:endpoints-security
if errorlevel 1 goto erro

echo [14/40] Fluxo web email...
call npm run email:web-flow
if errorlevel 1 goto erro

echo [15/40] Contrato comunicação...
call npm run api:communication-contract
if errorlevel 1 goto erro

echo [16/40] Certificado enriquecido...
call npm run certificate:rich-content
if errorlevel 1 goto erro

echo [17/40] Paridade Valora.Web pesquisa grátis...
call npm run web:free-survey-parity
if errorlevel 1 goto erro

echo [18/40] Web ASP.NET...
call npm run web:aspnet-only
if errorlevel 1 goto erro

echo [19/40] Web build...
call npm run web:build
if errorlevel 1 goto erro

echo [20/40] API sem rotas duplicadas...
call npm run api:no-route-conflicts
if errorlevel 1 goto erro

echo [21/40] Swagger health...
call npm run api:swagger-health
if errorlevel 1 goto erro

echo [22/40] SaaS e2e...
call npm run prod:saas-e2e
if errorlevel 1 goto erro

echo [23/40] Web e2e...
call npm run web:e2e
if errorlevel 1 goto erro

echo [24/40] Release final...
call npm run prod:final-gate
if errorlevel 1 goto erro

echo [25/40] Documento fluxo comercial...
if not exist HOME_FREE_SURVEY_COMMERCIAL_FLOW.md goto erro

echo [26/40] Documento email obrigatorio...
if not exist FREE_SURVEY_RESULT_EMAIL_RULE.md goto erro

echo [27/40] Documento SMTP...
if not exist EMAIL_SMTP_CONFIGURATION.md goto erro

echo [28/40] Documento certificado...
if not exist CERTIFICATE_RICH_CONTENT.md goto erro

echo [29/40] Documento migração...
if not exist SPRINT_46_MIGRATION_PROGRESS.md goto erro

echo PESQUISA GRATIS + EMAIL + WHATSAPP + CERTIFICADO VALIDADO.
pause
exit /b 0

:erro
echo FALHA NA VALIDACAO DA PESQUISA GRATIS/EMAIL/WHATSAPP/CERTIFICADO.
pause
exit /b 1

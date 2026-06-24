@echo off
setlocal EnableExtensions EnableDelayedExpansion
set REPORT=test-results\release-candidate.json
call :run "npm run check" npm run check || goto :fail
call :run "validate-firebase-config" node scripts\validate-firebase-config.js || goto :fail
call :run "validate-firebase-bundle" node scripts\validate-firebase-bundle.js || goto :fail
call :run "validate-faq-rendering" node scripts\validate-faq-rendering.js || goto :fail
call :run "validate-admin-access" node scripts\validate-admin-access.js || goto :fail
call :run "validate-certificates" node scripts\validate-certificates.js || goto :fail
call :run "validate-communication-flow" node scripts\validate-communication-flow.js || goto :fail
call :run "validate-gateway-integration" node scripts\validate-gateway-integration.js || goto :fail
call :run "audit-plan-capabilities" node scripts\audit-plan-capabilities.js || goto :fail
call :run "validate-plan-contracts" node scripts\validate-plan-contracts.js || goto :fail
call :run "validate-chatbot-natural-language" node scripts\validate-chatbot-natural-language.js || goto :fail
call :run "validate-release-candidate" node scripts\validate-release-candidate.js || goto :fail
call :run "build:prod" npm run build:prod || goto :fail
call :run "backup IIS" node scripts\publish-iis-prd.js --backup-only --mode firebase || goto :fail
call :run "publicar IIS" npm run publish:iis || goto :fail
call :run "iisreset" iisreset || goto :fail
call :run "healthcheck PRD" npm run prod:health || goto :fail
echo Homologacao final concluida. Relatorio: %REPORT%
exit /b 0
:run
echo === %~1 ===
shift
%*
exit /b %ERRORLEVEL%
:fail
echo FALHA na etapa anterior. Publicacao interrompida quando aplicavel. Relatorio: %REPORT%
exit /b 1

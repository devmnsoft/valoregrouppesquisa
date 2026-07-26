@echo off
cd /d C:\DBBACK\valoregrouppesquisa
call npm run local:live:up || goto erro
call npm run local:migrations:apply || goto erro
call npm run web:aspnet-only || goto erro
call npm run backend:build || goto erro
call npm run backend:test || goto erro
call npm run web:build || goto erro
call npm run web:validate || goto erro
call npm run web:dynamic-config || goto erro
call npm run web:health || goto erro
call npm run web:no-data-access || goto erro
call npm run web:no-binaries || goto erro
call npm run web:no-placeholders || goto erro
call npm run web:no-generic-official-render || goto erro
call npm run web:specific-renderers || goto erro
call npm run web:no-generic-admin || goto erro
call npm run web:controlled-gaps || goto erro
call npm run web:production-gaps || goto erro
call npm run web:page-specific-js || goto erro
call npm run web:mvc-logging || goto erro
call npm run web:real-forms || goto erro
call npm run web:real-api-usage || goto erro
call npm run web:authenticated-modules || goto erro
call npm run web:crud-api-gaps || goto erro
call npm run web:auth-guards || goto erro
call npm run web:no-json-dump || goto erro
call npm run web:admin-ux || goto erro
call npm run web:api-endpoints || goto erro
call npm run web:admin-security || goto erro
call npm run web:no-sensitive-ui || goto erro
call npm run web:production-security || goto erro
call npm run web:observability || goto erro
call npm run web:result-token-safety || goto erro
call npm run web:certificate-binary || goto erro
call npm run web:jquery || goto erro
call npm run web:api-contract || goto erro
call npm run web:screens || goto erro
call npm run web:errors || goto erro
call npm run web:mobile || goto erro
call npm run web:cors || goto erro
call npm run web:docker || goto erro
call npm run web:docker-runtime || goto erro
call npm run web:docker-smoke || goto erro
call npm run web:iis-readiness || goto erro
call npm run web:iis-publish-dry || goto erro
call npm run web:published-smoke || goto erro
call npm run web:functional-flow || goto erro
call npm run web:official-release || goto erro
call npm run web:final-release-gate || goto erro
call npm run backend:health || goto erro
call npm run api:e2e || goto erro
call npm run prod:saas-e2e || goto erro
call npm run web:e2e || goto erro
call npm run prod:frontend-saas || goto erro
call npm run security:check || goto erro
call npm run api:no-sensitive-logs || goto erro
call npm run prod:saas-readiness || goto erro
call npm run prod:no-legacy || goto erro
call npm run prod:no-pending || goto erro
call npm run prod:billing || goto erro
call npm run prod:email-flow || goto erro
call npm run prod:certificate-flow || goto erro
call npm run prod:performance || goto erro
call npm run prod:security-live || goto erro
call npm run prod:smoke || goto erro
call npm run prod:cutover-dry-run || goto erro
call npm run prod:rollback-ready || goto erro
docker compose build || goto erro
call npm run build:prod || goto erro
call npm run prod:final-gate || goto erro
if not exist backend\Valora.Web\Controllers\WebConfigController.cs goto erro
if not exist backend\Valora.Web\Controllers\WebHealthController.cs goto erro
if not exist SPRINT_42_VALORA_WEB_PRODUCTION_HOMOLOGATION_AUDIT.md goto erro
if not exist RELEASE_CANDIDATE_REPORT.md goto erro
call npm run local:live:down
echo VALORA.WEB ASP.NET + API + SAAS RELEASE FINAL VALIDADO.
pause
exit /b 0
:erro
echo FALHA NA VALIDACAO FINAL DO SAAS VALORA.WEB ASP.NET.
call npm run local:live:down
pause
exit /b 1

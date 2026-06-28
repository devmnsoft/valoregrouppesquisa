@echo off
setlocal enabledelayedexpansion
set GATES=local:live:up local:migrations:apply check security:check validate:data-contracts validate:render-resilience audit:data-shapes frontend:bootstrap frontend:api-errors prod:frontend-saas prod:browser-e2e api:error-handling api:logging api:repository-logging api:migration-logging api:no-sensitive-logs api:correlation api:transaction-logging api:email-errors api:health-observability api:provider journey:provider api:routes api:public-real api:typed-services api:service-size api:result-token api:transactional-submit api:repository-transaction architecture:warnings cutover:ready backend:implementation backend:clean postgres:schema runtime:docker-windows prod:saas-readiness prod:no-legacy prod:no-pending prod:saas-features prod:auth-flow prod:certificate-flow prod:email-flow prod:billing prod:security-gate prod:security-live backend:build backend:test backend:health postgres:mvp api:e2e prod:saas-e2e hybrid:check migration:dry-run-real migration:validate migration:compare prod:cutover-dry-run prod:rollback-ready prod:performance build:prod prod:health prod:bug-bash prod:final-gate local:live:down
for %%G in (%GATES%) do (
  echo [VALORA] npm run %%G
  npm run %%G
  if errorlevel 1 (
    echo [VALORA] Falha em %%G. Tentando derrubar ambiente live local...
    npm run local:live:down
    exit /b 1
  )
)
endlocal

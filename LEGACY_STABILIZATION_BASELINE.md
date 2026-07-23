# Legacy Stabilization Baseline — Valora Pulse / Valora Insight™

- Initial SHA before changes: `75f7ca7d36344cd5083d08410bd3aae644bda7dc`.
- Date: 2026-07-23.
- Branch base requested: `main`.

## Inventory summary

### Public routes and flows
- Public survey participation is mediated by `validateSurveyLink`, `submitSurveyResponse`, `getFeaturedHomeSurvey` and `repairFeaturedHomeSurvey`.
- Public result access keeps the v2 contract `?result=<responseId>&rt=<rawToken>` and is mediated by `getPublicResult`.
- Result resend and recovery use `sendResultEmail`, `requestNewResultLink` and `getParticipantResultsByPassword`.
- Certificate journeys remain in the legacy frontend/backend parity backlog until a single server-side certificate view model is completed.

### Administrative routes, profiles and roles
- Legacy roles observed: `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante`, `convidado_externo`.
- Administrative data commands are increasingly routed through callable Functions including client, survey, form, user, response, invitation, action plan, billing and repair operations.
- The browser must not create organization documents directly for production registration; `registerCompanyAccount` is now the server boundary for new company registration.

### Cloud Functions and scheduled Functions
- Critical public result token group: `submitSurveyResponse`, `getPublicResult`, `adminCreateResultShareLink`, `adminRegenerateResultLink`, `requestNewResultLink`, `sendResultEmail`, `getParticipantResultsByPassword`.
- Critical public survey token group: `preparePublicSurveyLink`, `validateSurveyLink`, `submitSurveyResponse`, `getFeaturedHomeSurvey`, `repairFeaturedHomeSurvey`.
- Scheduled Functions still require staged hardening for notification generation, reminders, invitation expiry and overdue actions; they are tracked in `LEGACY_EVOLUTION_BACKLOG.md`.

### Firestore collections and rules
- Collections touched by P0 result-token stabilization: `responses`, `responses/{responseId}/resultAccessTokens`, `public_result_access_errors`, `email_logs`, `audit_logs`.
- Company registration collections: `organizations` (canonical), `companies` (compatibility), `organizationSettings`, `users`, Firebase Auth custom claims and `audit_logs`.
- Firestore rules remain deny-by-default and require further role-specific hardening for knowledge base, support tickets, integrations, webhooks and token subcollections.

### Indexes, migrations and scripts
- Firestore indexes are declared in `firestore.indexes.json`.
- Plan catalog synchronization uses `scripts/sync-functions-plan-catalog.js` and `shared/plan-catalog.json`.
- Release group validation is now enforced by `scripts/validate-release-groups.js` and `functions/release-groups.json`.

### E-mail, tokens, integrations, webhooks and billing
- Result e-mail delivery is essential/transactional and must not be treated as marketing consent.
- Raw result tokens are returned only to the participant/link generator and are not written to diagnostics; hashes use SHA-256 document IDs under `resultAccessTokens`.
- Billing remains manual unless a real provider with signed webhook configuration is present; no fake checkout success is acceptable.
- API keys/webhooks need continued versioned hardening for scopes, HMAC signatures, SSRF protection, retries and dead-letter queues.

### Backend .NET and PostgreSQL
- Official strategic backend remains `backend/Valora.sln` with `Valora.Api`, `Valora.Application`, `Valora.Domain`, `Valora.Infrastructure`, `Valora.Web` and `database/postgresql`.
- No cutover is approved by this stabilization. See `LEGACY_TO_BACKEND_PARITY_MATRIX.md`.

## Duplicate/redefined contracts identified

- `verifyAndTouchResultAccess` returns an object with `ok`; callers must not treat the object itself as a boolean.
- Result access currently has a rolling-deploy compatibility bridge in `responses.resultTokenHash`; subcollection `responses/{responseId}/resultAccessTokens/{sha256(rawToken)}` is authoritative for v2.
- Legacy browser registration previously created Auth/user/company data directly from the frontend; production registration is now routed to `registerCompanyAccount`.
- Plan limit naming includes legacy formats such as `maxEmailsMonth`; backlog requires full removal in favor of `monthlyEmails` from the shared catalog.

## Risks found

1. Isolated Function deploys can break public result/token contracts if Hosting or a single callable is released independently.
2. Company registration compensation must delete Auth users and Firestore partials if later setup fails.
3. Scheduled notification functions still need real batch/cursor/lease implementations.
4. Firestore rules need deeper role-matrix coverage for support, knowledge base, integrations and webhooks.
5. Backend parity is incomplete; cutover requires dry-run, backup, volume test, pilot and explicit confirmation.

## Functions that must be published together

See `functions/release-groups.json`:
- `result-token-v2`
- `survey-token-v2`

## Rollback plan

1. Stop release if tests, Function smoke tests or release group validation fail.
2. If Functions were deployed but Hosting was not, redeploy the previous known-good Function group using the release evidence artifact.
3. If Hosting was deployed and smoke tests fail, roll back Firebase Hosting to the previous release and redeploy the previous known-good Function group.
4. Preserve data; do not run destructive migrations during rollback.
5. Keep `responses.resultTokenHash` bridge until telemetry proves all active bundles use v2 subcollection tokens.

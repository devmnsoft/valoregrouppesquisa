# Legacy Evolution Backlog

This backlog records work intentionally not claimed as production-homologated in this P0 stabilization.

## P0/P1 remaining hardening
- Complete Firestore role matrix tests for every listed role and cross-company access.
- Move all legacy global saves (`saveChanges`, collection array sync) to explicit server commands where still present.
- Finish transactional entitlement reservations for all listed limits using `organizationUsage/{companyId_YYYY_MM}`.
- Implement survey token v2 subcollection migration and cleanup window for raw survey tokens.
- Replace scheduled notification placeholders with lease/cursor/idempotent processors.
- Implement a single real billing provider after commercial/credential decision; otherwise keep UI as “Cobrança manual”.
- Version and modularize the public API with cursor pagination, minimization, scopes and idempotency.
- Complete certificate server-side registry, validation endpoint, QR code and PDF generation in the official backend.
- Continue progressive modularization of `app.js` and `functions/index.js` without big-bang rewrite.

# Legacy Release and Rollback Runbook

## Required validation commands

```bash
npm ci
npm --prefix functions ci
node scripts/sync-functions-plan-catalog.js
npm run release:groups:check
npm run check
npm run security:check
npm run test:rules
npm run test:functions
npm run test:security
npm run build:prod
```

## Production release sequence

1. Validate approved SHA and branch/tag.
2. Build artifact and save `functions/release-groups.json` in release evidence.
3. Deploy Firestore rules first.
4. Deploy all Functions in each affected release group.
5. Run Function smoke tests.
6. Deploy Hosting only after required Functions succeed.
7. Run site smoke tests and record evidence.
8. Keep previous release artifact for rollback.

## Administrative override

Set `RELEASE_GROUP_OVERRIDE=true` only for an audited emergency and attach `release-evidence/release-group-override.json` with reason, approver, functions excluded and rollback window.

## Rollback

- Hosting: use Firebase Hosting release rollback to the last known-good version.
- Functions: redeploy the previous known-good release group manifest.
- Data: do not delete or mutate real data during rollback; compatibility bridges remain active.

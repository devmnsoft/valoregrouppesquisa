#!/usr/bin/env bash
set -euo pipefail
npm run backend:official-validate
npm run backend:reports-email-validate
npm run backend:migration-import-validate
npm run backend:homologation-cutover-validate
npm run check:critical

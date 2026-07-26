#!/usr/bin/env bash
set -euo pipefail
[ -f .env ] && set -a && . ./.env && set +a
if [ "${CONFIRM_RESTORE:-}" != "RESTORE_LOCAL_HML" ]; then echo "Defina CONFIRM_RESTORE=RESTORE_LOCAL_HML para confirmar restore destrutivo local/hml."; exit 2; fi
if [ "${VALORA_ENVIRONMENT:-Homologation}" = "Production" ] && [ "${CONFIRM_PRODUCTION_RESTORE:-}" != "true" ]; then echo "Restore em produção bloqueado sem CONFIRM_PRODUCTION_RESTORE=true"; exit 3; fi
: "${VALORA_RESTORE_FILE:?Informe VALORA_RESTORE_FILE}"; : "${VALORA_POSTGRES_DB:=valora_hml}"; : "${VALORA_POSTGRES_USER:=valora_hml}"; : "${VALORA_POSTGRES_HOST:=localhost}"; : "${VALORA_POSTGRES_PORT:=5432}"
export PGPASSWORD="${VALORA_POSTGRES_PASSWORD:-change-me-local-only}"
pg_restore -h "$VALORA_POSTGRES_HOST" -p "$VALORA_POSTGRES_PORT" -U "$VALORA_POSTGRES_USER" -d "$VALORA_POSTGRES_DB" --clean --if-exists "$VALORA_RESTORE_FILE"

#!/usr/bin/env bash
set -euo pipefail
[ -f .env ] && set -a && . ./.env && set +a
: "${VALORA_POSTGRES_DB:=valora_hml}"; : "${VALORA_POSTGRES_USER:=valora_hml}"; : "${VALORA_POSTGRES_HOST:=localhost}"; : "${VALORA_POSTGRES_PORT:=5432}"
export PGPASSWORD="${VALORA_POSTGRES_PASSWORD:-change-me-local-only}"
psql -h "$VALORA_POSTGRES_HOST" -p "$VALORA_POSTGRES_PORT" -U "$VALORA_POSTGRES_USER" -d "$VALORA_POSTGRES_DB" -v ON_ERROR_STOP=1 -f database/postgresql/scriptbd_completo.sql
for f in database/postgresql/[0-9][0-9][0-9]_*.sql; do psql -h "$VALORA_POSTGRES_HOST" -p "$VALORA_POSTGRES_PORT" -U "$VALORA_POSTGRES_USER" -d "$VALORA_POSTGRES_DB" -v ON_ERROR_STOP=1 -f "$f"; done

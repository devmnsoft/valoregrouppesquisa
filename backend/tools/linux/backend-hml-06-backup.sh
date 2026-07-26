#!/usr/bin/env bash
set -euo pipefail
[ -f .env ] && set -a && . ./.env && set +a
: "${VALORA_BACKUP_DIR:=./backups/postgresql}"; mkdir -p "$VALORA_BACKUP_DIR"
: "${VALORA_POSTGRES_DB:=valora_hml}"; : "${VALORA_POSTGRES_USER:=valora_hml}"; : "${VALORA_POSTGRES_HOST:=localhost}"; : "${VALORA_POSTGRES_PORT:=5432}"
export PGPASSWORD="${VALORA_POSTGRES_PASSWORD:-change-me-local-only}"
out="$VALORA_BACKUP_DIR/valora_${VALORA_POSTGRES_DB}_$(date -u +%Y%m%dT%H%M%SZ).dump"
pg_dump -h "$VALORA_POSTGRES_HOST" -p "$VALORA_POSTGRES_PORT" -U "$VALORA_POSTGRES_USER" -d "$VALORA_POSTGRES_DB" -Fc -f "$out"
test -s "$out"
printf 'Backup criado: %s\nComando: pg_dump -Fc -f <arquivo>\n' "$out" | tee -a "$VALORA_BACKUP_DIR/backup.log"

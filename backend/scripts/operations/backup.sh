#!/usr/bin/env bash
set -euo pipefail

: "${VALORA_DATABASE_URL:?Defina VALORA_DATABASE_URL sem gravar credenciais no script}"
backup_dir="${VALORA_BACKUP_DIR:-./backups}"
files_dir="${VALORA_FILES_DIR:-}"
retention_days="${VALORA_BACKUP_RETENTION_DAYS:-30}"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "$backup_dir"
umask 077

pg_dump --format=custom --no-owner --no-acl --file "$backup_dir/valora-$timestamp.dump" "$VALORA_DATABASE_URL"
if [[ -n "$files_dir" && -d "$files_dir" ]]; then
  tar --create --gzip --file "$backup_dir/valora-files-$timestamp.tar.gz" --directory "$files_dir" .
fi
sha256sum "$backup_dir"/valora-*"$timestamp"* > "$backup_dir/valora-$timestamp.sha256"
find "$backup_dir" -type f -name 'valora-*' -mtime "+$retention_days" -delete
printf 'Backup concluído em %s\n' "$backup_dir"

#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"
VERSION="$(tr -d '\r\n' < VERSION)"
PKG_DIR="publish/package/valora-${VERSION}"
rm -rf "$PKG_DIR"
mkdir -p "$PKG_DIR/sql" "$PKG_DIR/docs"
cp -R "publish/valora-${VERSION}/api" "$PKG_DIR/api"
cp -R "publish/valora-${VERSION}/web" "$PKG_DIR/web"
cp -R database/postgresql/*.sql "$PKG_DIR/sql/"
cp VERSION RELEASE_CANDIDATE_NOTES.md CUTOVER_PLAN.md ROLLBACK_PLAN.md BACKUP_RESTORE_RUNBOOK.md SECURITY_HARDENING_CHECKLIST.md "$PKG_DIR/docs/"
find "$PKG_DIR" -type f \( -name '.env' -o -name '*.dump' -o -name '*.bak' -o -name '*.log' \) -print -quit | grep -q . && { echo 'Pacote contém artefato proibido'; exit 1; } || true
( cd "$PKG_DIR" && find . -type f -print0 | sort -z | xargs -0 sha256sum ) > "$PKG_DIR/ARTIFACTS.sha256"
tar -czf "publish/valora-${VERSION}.tar.gz" -C "publish/package" "valora-${VERSION}"
echo "Pacote criado: publish/valora-${VERSION}.tar.gz"

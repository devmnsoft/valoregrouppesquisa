#!/usr/bin/env bash
set -euo pipefail

: "${ConnectionStrings__DefaultConnection:?Defina ConnectionStrings__DefaultConnection antes de preparar o banco.}"
command -v psql >/dev/null || { echo "psql não foi encontrado no PATH." >&2; exit 1; }
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
psql "$ConnectionStrings__DefaultConnection" -v ON_ERROR_STOP=1 -f "$root/database/postgresql/script_completo.sql"

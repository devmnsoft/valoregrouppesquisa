#!/usr/bin/env bash
set -euo pipefail

: "${VALORA_PSQL_CONNECTION:?Defina VALORA_PSQL_CONNECTION com uma URI PostgreSQL ou conninfo libpq antes de preparar o banco.}"
command -v psql >/dev/null || { echo "psql não foi encontrado no PATH." >&2; exit 1; }
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
psql "$VALORA_PSQL_CONNECTION" -X -v ON_ERROR_STOP=1 -f "$root/database/postgresql/script_completo.sql"

#!/usr/bin/env bash
set -euo pipefail

: "${ConnectionStrings__DefaultConnection:?Defina ConnectionStrings__DefaultConnection antes de preparar o banco.}"
command -v psql >/dev/null || { echo "psql não foi encontrado no PATH." >&2; exit 1; }
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
psql "$ConnectionStrings__DefaultConnection" -v ON_ERROR_STOP=1 -f "$root/database/postgresql/script_completo.sql"

if [[ "${VALORA_SEED_DEMO:-false}" == "true" ]]; then
  [[ "${ASPNETCORE_ENVIRONMENT:-}" == "Development" ]] || {
    echo "VALORA_SEED_DEMO só pode ser usado com ASPNETCORE_ENVIRONMENT=Development." >&2; exit 1;
  }
  psql "$ConnectionStrings__DefaultConnection" -v ON_ERROR_STOP=1 -f "$root/database/postgresql/seeds/seed_demo.sql"
  echo "Massa demo local aplicada. Login: admin.demo@valora.local"
fi

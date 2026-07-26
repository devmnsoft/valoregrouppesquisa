#!/usr/bin/env bash
set -euo pipefail
: "${VALORA_API_URL:=http://localhost:5080}"
for path in /health /health/database /health/migration /health/email /health/storage /health/version; do
  curl -fsS "$VALORA_API_URL$path" >/dev/null
  echo "OK $path"
done

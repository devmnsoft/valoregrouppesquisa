#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"
OUT="publish/valora-0.9.0-rc1/web"
rm -rf "$OUT"
dotnet publish backend/Valora.Web/Valora.Web.csproj -c Release -o "$OUT" /p:UseAppHost=false

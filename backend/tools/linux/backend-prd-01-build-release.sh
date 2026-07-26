#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"
dotnet restore backend/Valora.sln
dotnet build backend/Valora.sln -c Release --no-restore

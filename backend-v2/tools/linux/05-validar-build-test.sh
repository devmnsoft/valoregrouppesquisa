#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
dotnet build ValoraPesquisa.sln
dotnet test ValoraPesquisa.sln
node tools/validate-backend-v2-foundation.js

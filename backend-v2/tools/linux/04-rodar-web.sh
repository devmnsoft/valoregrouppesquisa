#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
dotnet run --project src/ValoraPesquisa.Web/ValoraPesquisa.Web.csproj

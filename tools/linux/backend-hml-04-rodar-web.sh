#!/usr/bin/env bash
set -euo pipefail
dotnet run --project backend/Valora.Web/Valora.Web.csproj --urls http://localhost:5088

#!/usr/bin/env bash
set -euo pipefail
dotnet run --project backend/Valora.Api/Valora.Api.csproj --urls http://localhost:5080

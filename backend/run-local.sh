#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

cleanup() { kill "${api_pid:-}" "${web_pid:-}" 2>/dev/null || true; }
trap cleanup EXIT INT TERM

ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --project Valora.Api/Valora.Api.csproj --urls http://localhost:5080 &
api_pid=$!
ASPNETCORE_ENVIRONMENT=Development Api__BaseUrl=http://localhost:5080 dotnet run --no-launch-profile --project Valora.Web/Valora.Web.csproj --urls "http://localhost:5088;https://localhost:7088" &
web_pid=$!

printf 'Valora.Api: http://localhost:5080 | Valora.Web: http://localhost:5088 e https://localhost:7088\n'
wait -n "$api_pid" "$web_pid"

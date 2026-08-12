#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

cleanup() { kill "${api_pid:-}" "${web_pid:-}" 2>/dev/null || true; }
trap cleanup EXIT INT TERM

ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --project Valora.Api/Valora.Api.csproj --urls http://localhost:5080 &
api_pid=$!

printf 'Aguardando Valora.Api em http://localhost:5080/health'
for attempt in {1..60}; do
  if curl --fail --silent --show-error --max-time 2 http://localhost:5080/health >/dev/null 2>&1; then
    printf ' pronta.\n'
    break
  fi
  if ! kill -0 "$api_pid" 2>/dev/null; then
    printf '\nValora.Api encerrou antes de ficar pronta.\n' >&2
    wait "$api_pid"
  fi
  if [[ "$attempt" -eq 60 ]]; then
    printf '\nTempo esgotado aguardando Valora.Api. Consulte o log acima.\n' >&2
    exit 1
  fi
  printf '.'
  sleep 1
done

ASPNETCORE_ENVIRONMENT=Development Api__BaseUrl=http://localhost:5080 dotnet run --no-launch-profile --project Valora.Web/Valora.Web.csproj --urls "http://localhost:5088;https://localhost:7088" &
web_pid=$!

printf 'Valora.Api: http://localhost:5080 | Valora.Web: http://localhost:5088 e https://localhost:7088\n'
wait -n "$api_pid" "$web_pid"

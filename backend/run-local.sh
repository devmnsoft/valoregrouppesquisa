#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

api_url="http://localhost:5080"
web_http_url="http://localhost:5088"
web_https_url="https://localhost:7088"
started_pids=()

port_listeners() {
  local port="$1"
  if command -v lsof >/dev/null 2>&1; then
    local pids pid
    pids="$(lsof -nP -t -iTCP:"$port" -sTCP:LISTEN 2>/dev/null || true)"
    for pid in $pids; do
      ps -p "$pid" -o pid=,comm=,args= 2>/dev/null || true
    done
    return
  fi
  if command -v ss >/dev/null 2>&1; then
    ss -ltnp "sport = :$port" 2>/dev/null || true
    return
  fi
  netstat -ltnp 2>/dev/null | awk -v port=":$port" '$4 ~ port { print }' || true
}

assert_port_free() {
  local port="$1" service="$2" listeners
  listeners="$(port_listeners "$port")"
  if [[ -n "$listeners" ]]; then
    printf 'Nao foi possivel iniciar o %s: a porta %s esta ocupada. Encerre a instancia anterior ou revise a configuracao de inicializacao.\n' "$service" "$port" >&2
    printf 'Listeners encontrados:\n%s\n' "$listeners" >&2
    exit 1
  fi
}

cleanup() {
  local pid
  for pid in "${started_pids[@]:-}"; do
    if kill -0 "$pid" 2>/dev/null; then
      pkill -TERM -P "$pid" 2>/dev/null || true
      kill "$pid" 2>/dev/null || true
    fi
  done
}
trap cleanup EXIT INT TERM

assert_port_free 5080 "Valora.Api"
assert_port_free 5088 "Valora.Web"
assert_port_free 7088 "Valora.Web"

ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --project Valora.Api/Valora.Api.csproj --urls "$api_url" &
api_pid=$!
started_pids+=("$api_pid")

printf 'Aguardando Valora.Api em %s/health' "$api_url"
for attempt in {1..60}; do
  if ! kill -0 "$api_pid" 2>/dev/null; then
    printf '\nValora.Api encerrou antes de ficar pronta.\n' >&2
    wait "$api_pid"
  fi
  if curl --fail --silent --show-error --max-time 2 "$api_url/health" >/dev/null 2>&1; then
    listeners="$(port_listeners 5080)"
    if [[ "$listeners" == *"Valora.Api"* ]]; then
      printf ' pronta.\n'
      break
    fi
    printf '\nA porta 5080 respondeu, mas nao foi possivel confirmar que pertence a Valora.Api.\n%s\n' "$listeners" >&2
    exit 1
  fi
  if [[ "$attempt" -eq 60 ]]; then
    printf '\nTempo esgotado aguardando Valora.Api. Consulte o log acima.\n' >&2
    exit 1
  fi
  printf '.'
  sleep 1
done

ASPNETCORE_ENVIRONMENT=Development Api__BaseUrl="$api_url" dotnet run --no-launch-profile --project Valora.Web/Valora.Web.csproj --urls "$web_http_url;$web_https_url" &
web_pid=$!
started_pids+=("$web_pid")

printf 'Aguardando Valora.Web em %s e %s' "$web_http_url" "$web_https_url"
for attempt in {1..60}; do
  if ! kill -0 "$web_pid" 2>/dev/null; then
    printf '\nValora.Web encerrou antes de ficar pronta.\n' >&2
    wait "$web_pid"
  fi
  http_listeners="$(port_listeners 5088)"
  https_listeners="$(port_listeners 7088)"
  if [[ "$http_listeners" == *"Valora.Web"* && "$https_listeners" == *"Valora.Web"* ]]; then
    printf ' pronta.\n'
    break
  fi
  if [[ "$attempt" -eq 60 ]]; then
    printf '\nTempo esgotado aguardando Valora.Web nas portas 5088 e 7088.\n' >&2
    printf '5088:\n%s\n7088:\n%s\n' "$http_listeners" "$https_listeners" >&2
    exit 1
  fi
  printf '.'
  sleep 1
done

printf 'Valora.Api: %s | Valora.Web: %s e %s\n' "$api_url" "$web_http_url" "$web_https_url"
printf 'Pressione Ctrl+C para encerrar somente os processos iniciados por este script.\n'
wait -n "$api_pid" "$web_pid"

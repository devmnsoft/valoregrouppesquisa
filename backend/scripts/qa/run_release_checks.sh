#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
repo="$(cd "$root/.." && pwd)"
cd "$root"

for command in dotnet node; do
  command -v "$command" >/dev/null || { echo "ERRO: $command não foi encontrado no PATH." >&2; exit 127; }
done

echo "[1/6] Auditoria estática de marcadores e ações vazias"
if rg -n --glob '*.cshtml' \
  "href\\s*=\\s*['\"](#|javascript:void\\(0\\))?['\"]" Valora.Web; then
  echo "ERRO: links sem destino foram encontrados." >&2
  exit 1
fi
if rg -n --glob '!artifacts/**' --glob '!**/bin/**' --glob '!**/obj/**' \
  'throw new NotImplementedException' Valora.Api Valora.Application Valora.Domain Valora.Infrastructure Valora.Web; then
  echo "ERRO: implementação incompleta encontrada em código de produção." >&2
  exit 1
fi

echo "[2/6] Restore"
dotnet restore Valora.sln --nologo
echo "[3/6] Build Release"
dotnet build Valora.sln -c Release --no-restore --nologo --warnaserror
echo "[4/6] Testes"
dotnet test Valora.sln -c Release --no-build --nologo
echo "[5/6] Contratos SQL estáticos"
(cd "$repo" && npm run db:scriptbd-validate)
echo "[6/6] Publish"
dotnet publish Valora.Api/Valora.Api.csproj -c Release --no-restore --nologo -o artifacts/release/api
dotnet publish Valora.Web/Valora.Web.csproj -c Release --no-restore --nologo -o artifacts/release/web

echo "Release Candidate: PASS"

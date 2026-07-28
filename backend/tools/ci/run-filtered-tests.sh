#!/usr/bin/env bash
set -uo pipefail

if [[ $# -ne 2 ]]; then
  echo "usage: $0 <category> <trx-file>" >&2
  exit 64
fi

category=$1
trx_file=$2
filter="Category=${category}"
started=$SECONDS

discovered=$(dotnet test Valora.sln --configuration Release --no-build --list-tests 2>&1)
printf '%s\n' "$discovered"
count=$(printf '%s\n' "$discovered" | sed -n '/The following Tests are available:/,$p' | tail -n +2 | sed '/^[[:space:]]*$/d' | wc -l)
if [[ $count -eq 0 ]]; then
  echo "No tests were discovered by Valora.sln." >&2
  exit 2
fi

matched=$(dotnet test Valora.sln --configuration Release --no-build --list-tests --filter "$filter" 2>&1)
printf '%s\n' "$matched"
matched_count=$(printf '%s\n' "$matched" | sed -n '/The following Tests are available:/,$p' | tail -n +2 | sed '/^[[:space:]]*$/d' | wc -l)
if [[ $matched_count -eq 0 ]]; then
  echo "No tests matched ${filter}." >&2
  exit 3
fi

set +e
dotnet test Valora.sln --configuration Release --no-build --filter "$filter" --logger "trx;LogFileName=${trx_file}"
status=$?
set -e
echo "Category ${category}: ${matched_count} tests; duration $((SECONDS - started))s; exit ${status}."
exit "$status"

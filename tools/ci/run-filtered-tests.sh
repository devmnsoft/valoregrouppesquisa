#!/usr/bin/env bash
set -euo pipefail

if (($# != 2)); then
  echo "usage: $0 <category> <trx-file>" >&2
  exit 64
fi

category=$1
trx_file=$(basename -- "$2")
project=${VALORA_TEST_PROJECT:-Valora.Tests/Valora.Tests.csproj}
project_dir=$(dirname -- "$project")
results_dir="$project_dir/TestResults"
filter="Category=$category"

if [[ ! -f $project ]]; then
  echo "Test project not found: $project" >&2
  exit 66
fi

mkdir -p "$results_dir"
list_output=$(mktemp)
trap 'rm -f "$list_output"' EXIT

echo "Discovering tests for category '$category' in $project"
dotnet test "$project" \
  --configuration Release \
  --list-tests \
  --filter "$filter" \
  --nologo >"$list_output"
cat "$list_output"

# VSTest prints discovered names indented after this marker. Counting only that
# section avoids mistaking restore/build diagnostics for tests.
mapfile -t tests < <(awk '
  /The following Tests are available:/ { listing=1; next }
  listing && /^[[:space:]]+[^[:space:]]/ {
    sub(/^[[:space:]]+/, ""); print
  }
' "$list_output")

if ((${#tests[@]} == 0)); then
  echo "No tests discovered for category '$category'." >&2
  exit 3
fi

printf 'Discovered %d test(s):\n' "${#tests[@]}"
printf '  - %s\n' "${tests[@]}"

rm -f "$results_dir/$trx_file"
dotnet test "$project" \
  --configuration Release \
  --filter "$filter" \
  --logger "trx;LogFileName=$trx_file" \
  --results-directory "$results_dir" \
  --nologo

trx_path="$results_dir/$trx_file"
if [[ ! -s $trx_path ]]; then
  echo "Expected non-empty TRX artifact was not produced: $trx_path" >&2
  exit 4
fi

echo "TRX artifact: $trx_path"

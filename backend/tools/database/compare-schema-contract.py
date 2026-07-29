#!/usr/bin/env python3
"""Compare deterministic JSON-lines schema contracts without hiding differences."""
from pathlib import Path
import sys

if len(sys.argv) != 3:
    raise SystemExit("usage: compare-schema-contract.py <expected> <actual>")

def records(path: str) -> set[str]:
    return {line.strip() for line in Path(path).read_text(encoding="utf-8").splitlines() if line.strip()}

expected, actual = map(records, sys.argv[1:])
if expected != actual:
    for value in sorted(expected - actual):
        print(f"- {value}")
    for value in sorted(actual - expected):
        print(f"+ {value}")
    raise SystemExit(1)
print(f"Schema contracts match ({len(actual)} records).")

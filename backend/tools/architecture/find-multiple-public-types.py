#!/usr/bin/env python3
"""Find C# production files containing multiple top-level public types."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

PROJECTS = ("Valora.Api", "Valora.Application", "Valora.Domain", "Valora.Infrastructure", "Valora.Web")
DECLARATION = re.compile(
    r"\bpublic\s+(?:(?:abstract|sealed|static|partial|readonly|ref)\s+)*"
    r"(?P<kind>class|record(?:\s+(?:class|struct))?|interface|enum)\s+(?P<name>[A-Za-z_]\w*)"
)


def sanitized(source: str) -> str:
    """Replace comments and literals with whitespace while retaining braces/newlines."""
    result = list(source)
    i = 0
    state = "code"
    while i < len(source):
        pair = source[i : i + 2]
        if state == "code" and pair == "//":
            result[i] = result[i + 1] = " "
            state = "line"
            i += 2
            continue
        if state == "code" and pair == "/*":
            result[i] = result[i + 1] = " "
            state = "block"
            i += 2
            continue
        if state == "code" and source[i] in ('"', "'"):
            quote = source[i]
            result[i] = " "
            state = quote
            i += 1
            continue
        if state == "line":
            if source[i] == "\n":
                state = "code"
            else:
                result[i] = " "
            i += 1
            continue
        if state == "block":
            if pair == "*/":
                result[i] = result[i + 1] = " "
                state = "code"
                i += 2
            else:
                if source[i] != "\n":
                    result[i] = " "
                i += 1
            continue
        if state in ('"', "'"):
            if source[i] == "\\" and i + 1 < len(source):
                result[i] = result[i + 1] = " "
                i += 2
            elif source[i] == state:
                result[i] = " "
                state = "code"
                i += 1
            else:
                if source[i] != "\n":
                    result[i] = " "
                i += 1
            continue
        i += 1
    return "".join(result)


def types_in(path: Path) -> list[dict[str, object]]:
    source = sanitized(path.read_text(encoding="utf-8-sig"))
    depth = 0
    depths = [0] * (len(source) + 1)
    for index, character in enumerate(source):
        depths[index] = depth
        if character == "{":
            depth += 1
        elif character == "}":
            depth = max(0, depth - 1)
    declarations = []
    for match in DECLARATION.finditer(source):
        # File-scoped namespaces have depth zero; block namespaces have depth one.
        prefix = source[: match.start()]
        namespace_depth = 0 if re.search(r"\bnamespace\s+[\w.]+\s*;", prefix) else 1
        if depths[match.start()] == namespace_depth:
            declarations.append(
                {
                    "name": match.group("name"),
                    "kind": match.group("kind"),
                    "line": source.count("\n", 0, match.start()) + 1,
                }
            )
    return declarations


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--json", type=Path, help="write the complete inventory as JSON")
    args = parser.parse_args()
    backend = Path(__file__).resolve().parents[2]
    scanned = 0
    violations = []
    for project in PROJECTS:
        for path in sorted((backend / project).rglob("*.cs")):
            if {"bin", "obj"} & set(path.parts) or path.name.endswith((".g.cs", ".generated.cs")):
                continue
            scanned += 1
            declarations = types_in(path)
            if len(declarations) > 1:
                violations.append({"file": path.relative_to(backend).as_posix(), "types": declarations})
    report = {"projects": list(PROJECTS), "filesScanned": scanned, "violationCount": len(violations), "violations": violations}
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    for violation in violations:
        found = ", ".join(f"{item['kind']} {item['name']} (line {item['line']})" for item in violation["types"])
        print(f"{violation['file']}: {found}")
    print(f"Scanned {scanned} files; found {len(violations)} violation(s).")
    return 1 if violations else 0


if __name__ == "__main__":
    sys.exit(main())

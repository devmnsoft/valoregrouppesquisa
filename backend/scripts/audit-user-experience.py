"""Read-only inventory; findings require review, not automatic failure claims."""

from pathlib import Path
import json
import re
import sys


def audit_views() -> None:
    try:
        root = Path(__file__).resolve().parents[1] / "Valora.Web"
        views_dir = root / "Views"
        if not views_dir.exists():
            print(f"Diretório de Views não encontrado em: {views_dir}", file=sys.stderr)
            return

        rows = []
        for p in sorted(views_dir.rglob("*.cshtml")):
            try:
                text = p.read_text(encoding="utf-8", errors="ignore")
                if "<form" not in text:
                    continue

                rows.append({
                    "page": str(p.relative_to(root)),
                    "forms": len(re.findall(r"<form\b", text)),
                    "required_fields": len(re.findall(r"\brequired\b", text)),
                    "validation_helpers": len(re.findall(r"asp-validation-for|asp-validation-summary", text)),
                    "manual_id_labels": re.findall(r"<label[^>]*>[^<]*(?:\bID\b|\bUUID\b)[^<]*", text),
                    "review": "Inspect API rules, rendered selectors, dynamic forms and actual persistence; HTML alone does not prove validation."
                })
            except Exception as read_err:
                print(f"Erro ao auditar arquivo {p}: {read_err}", file=sys.stderr)

        output_data = {
            "scope": "Razor forms only; dynamic JavaScript forms also require review",
            "pages": rows
        }
        print(json.dumps(output_data, ensure_ascii=False, indent=2))
    except Exception as ex:
        print(f"Erro inesperado durante auditoria: {ex}", file=sys.stderr)


if __name__ == "__main__":
    audit_views()

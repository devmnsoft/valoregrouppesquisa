"""Read-only inventory; findings require review, not automatic failure claims."""
from pathlib import Path
import json,re
root=Path(__file__).resolve().parents[1]/'Valora.Web'
rows=[]
for p in sorted((root/'Views').rglob('*.cshtml')):
 text=p.read_text()
 if '<form' not in text: continue
 rows.append({'page':str(p.relative_to(root)), 'forms':len(re.findall(r'<form\b',text)), 'required_fields':len(re.findall(r'\brequired\b',text)), 'validation_helpers':len(re.findall(r'asp-validation-for|asp-validation-summary',text)), 'manual_id_labels':re.findall(r'<label[^>]*>[^<]*(?:\bID\b|\bUUID\b)[^<]*',text), 'review':'Inspect API rules, rendered selectors, dynamic forms and actual persistence; HTML alone does not prove validation.'})
print(json.dumps({'scope':'Razor forms only; dynamic JavaScript forms also require review','pages':rows},ensure_ascii=False,indent=2))

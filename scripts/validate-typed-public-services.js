const fs=require('fs');const path=require('path');
const roots=['backend/Valora.Application/Services/PublicSurveys','backend/Valora.Application/Services/PublicResults'];
const forbidden=[/\bdynamic\b/,/ExpandoObject/,/private\s+static\s+dynamic/,/\(Guid\)survey\./,/\(string\)survey\./,/new\s+ValoraInsightCalculator\s*\(/];
let errors=[];function walk(d){for(const n of fs.readdirSync(d)){const p=path.join(d,n);const st=fs.statSync(p);if(st.isDirectory())walk(p);else if(p.endsWith('.cs')){const s=fs.readFileSync(p,'utf8');for(const r of forbidden)if(r.test(s))errors.push(`${p}: ${r}`);}}}
roots.forEach(walk);if(errors.length){console.error(errors.join('\n'));process.exit(1)}console.log('validate-typed-public-services: PASS');

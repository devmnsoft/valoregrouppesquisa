const fs=require('fs'),path=require('path');
function walk(d){return fs.existsSync(d)?fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>{const p=path.join(d,e.name);return e.isDirectory()?walk(p):[p]}):[]}
const files=walk('backend/Valora.Web').filter(f=>/\.(cshtml|js)$/.test(f));
const bad=[/ValoraOfficialPage\.load/,/ValoraAdminPage\.init/,/renderRows\s*\(/,/function\s+rows\s*\(/,/rows\s*\(\s*data\s*\)/,/Registro 1/,/Item 1/,/Dados carregados com renderização específica/,/Dados carregados pela API ou fallback controlado/,/Executar ação/,/Tela Bootstrap API-first/];
const hits=[]; for(const f of files){const s=fs.readFileSync(f,'utf8'); bad.forEach(r=>{if(r.test(s))hits.push(`${f}: ${r}`)});}
if(hits.length){console.error('Render genérico indevido no Valora.Web oficial:\n'+hits.join('\n'));process.exit(1)}
console.log('validate-aspnet-web-no-generic-official-render: PASS');

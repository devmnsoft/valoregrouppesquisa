const fs = require('fs');
const path = require('path');
const bad = ['JSON.stringify(result,null,2)','<pre','console.log(result)','console.log(payload)','console.log(token)','console.log(resultToken)'];
const fail = [];
function walk(d) { for (const e of fs.readdirSync(d, { withFileTypes: true })) { const p = path.join(d, e.name); if (e.isDirectory()) walk(p); else if (/\.(js|cshtml)$/.test(p)) { const s = fs.readFileSync(p, 'utf8'); for (const b of bad) if (s.includes(b)) fail.push(`${p}: ${b}`); } } }
walk('backend/Valora.Web');
if (fail.length) { console.error(fail.join('\n')); process.exit(1); }
console.log('Sem JSON bruto.');

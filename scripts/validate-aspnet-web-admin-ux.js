const fs = require('fs');
const path = require('path');
const bad = ['Executar ação','Tela Bootstrap API-first','Nenhum dado carregado ainda','Pronto para homologação','Identificador'];
const fail = [];
function walk(d) { for (const e of fs.readdirSync(d, { withFileTypes: true })) { const p = path.join(d, e.name); if (e.isDirectory()) walk(p); else if (/\.(js|cshtml)$/.test(p)) { const s = fs.readFileSync(p, 'utf8'); for (const b of bad) if (s.includes(b)) fail.push(`${p}: ${b}`); } } }
walk('backend/Valora.Web');
const req = ['empty-state','error-state','loading-state','table-toolbar','filter-panel','pagination','confirm-modal','status-badge','plan-limit-alert','copy-link-button'];
const all = fs.readFileSync('backend/Valora.Web/Views/Dashboard/Index.cshtml', 'utf8');
const miss = req.filter(r => !all.includes(r));
if (miss.length) fail.push('missing ux ' + miss.join(','));
if (fail.length) { console.error(fail.join('\n')); process.exit(1); }
console.log('UX admin OK.');

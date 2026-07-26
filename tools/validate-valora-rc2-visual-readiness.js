const fs = require('fs');
const path = require('path');
const root = process.cwd();
const failures = [];
const read = (file) => fs.existsSync(path.join(root, file)) ? fs.readFileSync(path.join(root, file), 'utf8') : '';
const exists = (file) => fs.existsSync(path.join(root, file));
const ok = (cond, msg) => { if (!cond) failures.push(msg); };
const files = {
  diagnostic: 'SPRINT_VALORA_RC2_VISUAL_BRAND_DIAGNOSTIC.md',
  checklist: 'VALORA_RC2_VISUAL_HOMOLOGATION_CHECKLIST.md',
  manual: 'VALORA_BRAND_ASSETS_MANUAL_SETUP.md',
  publicLayout: 'backend/Valora.Web/Views/Shared/_PublicLayout.cshtml',
  adminLayout: 'backend/Valora.Web/Views/Shared/_AdminLayout.cshtml',
  home: 'backend/Valora.Web/Views/Home/Index.cshtml',
  result: 'backend/Valora.Web/Views/Results/Public.cshtml',
  certDetails: 'backend/Valora.Web/Views/Certificates/Details.cshtml',
  certValidate: 'backend/Valora.Web/Views/Certificates/Validate.cshtml',
  sidebar: 'backend/Valora.Web/Views/Shared/_Sidebar.cshtml',
  topbar: 'backend/Valora.Web/Views/Shared/_Topbar.cshtml',
  cssPublic: 'backend/Valora.Web/wwwroot/css/valora-public.css',
  cssAdmin: 'backend/Valora.Web/wwwroot/css/valora-admin.css',
  sqlRoot: 'scriptbd_completo.sql',
  sqlDb: 'backend/database/postgresql/scriptbd_completo.sql'
};
ok(exists(files.diagnostic), 'Diagnóstico inicial RC2 não existe');
ok(exists(files.checklist), 'Checklist visual RC2 não existe');
ok(exists(files.manual), 'Manual de assets não existe');
const fallbackSources = [files.home, files.result, files.certDetails, files.certValidate, files.sidebar, files.topbar, files.cssPublic, files.cssAdmin].map(read).join('\n');
ok(/brand-fallback-text/.test(fallbackSources) && /brand-fallback-active/.test(fallbackSources), 'Fallback visual de marca não encontrado');
for (const [label, file] of Object.entries({ home: files.home, result: files.result, certDetails: files.certDetails, certValidate: files.certValidate, sidebar: files.sidebar, topbar: files.topbar })) {
  const text = read(file);
  ok(/\/img\/brand\/valora-(logo-full|symbol)\.jpeg/.test(text), `${label} não usa logo/símbolo oficial`);
  ok(/brand-fallback-text/.test(text), `${label} não possui fallback textual`);
}
ok(!/_Sidebar/.test(read(files.publicLayout)), 'Layout público referencia sidebar/admin');
ok(/auth-session|guards|logoutButton|_Sidebar|_Topbar/.test(read(files.adminLayout)), 'Layout admin não indica sessão/guard/login');
const sql = `${read(files.sqlRoot)}\n${read(files.sqlDb)}`;
ok(sql.includes('/img/brand/valora-logo-full.jpeg'), 'SQL não possui path da logo completa');
ok(sql.includes('/img/brand/valora-symbol.jpeg'), 'SQL não possui path do símbolo');
const scanTargets = ['backend/Valora.Web/Views', 'backend/Valora.Web/wwwroot/css', 'backend/Valora.Web/wwwroot/js', 'backend/database/postgresql', 'scriptbd_completo.sql'];
for (const target of scanTargets) {
  const full = path.join(root, target);
  if (!fs.existsSync(full)) continue;
  const stat = fs.statSync(full);
  const list = stat.isDirectory() ? walk(full) : [full];
  for (const fullFile of list) {
    if (!/\.(cshtml|css|js|json|sql|md|config|cs)$/i.test(fullFile)) continue;
    const rel = path.relative(root, fullFile).replace(/\\/g, '/');
    const text = fs.readFileSync(fullFile, 'utf8');
    ok(!/<img\b[^>]+src=["']https?:\/\//i.test(text), `${rel} usa imagem externa`);
    ok(!/-----BEGIN PRIVATE KEY-----|"private_key"\s*:|firebase-adminsdk|"type"\s*:\s*"service_account"|"client_email"\s*:/i.test(text), `${rel} contém padrão de secret/service account`);
    ok(!/\.env(\.production|\.local)?\b/i.test(rel) || /\.example$|README|AUDIT|CHECKLIST|SETUP/i.test(rel), `${rel} parece .env real`);
  }
}
function walk(dir) {
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (['node_modules', '.git', 'bin', 'obj', 'dist', 'coverage'].includes(entry.name)) continue;
    const p = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(p)); else out.push(p);
  }
  return out;
}
if (failures.length) { console.error('validate-valora-rc2-visual-readiness: FAIL\n' + failures.join('\n')); process.exit(1); }
console.log('validate-valora-rc2-visual-readiness: PASS');

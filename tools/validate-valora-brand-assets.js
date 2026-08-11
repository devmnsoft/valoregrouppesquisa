const fs = require('fs');
const path = require('path');
const root = process.cwd();
const fail = [];
const warn = [];
const allowMissing = String(process.env.VALORA_ALLOW_MISSING_BRAND_ASSETS || '').toLowerCase() === 'true';
const read = (file) => fs.existsSync(path.join(root, file)) ? fs.readFileSync(path.join(root, file), 'utf8') : '';
const exists = (file) => fs.existsSync(path.join(root, file));
const ok = (condition, message) => { if (!condition) fail.push(message); };
const logo = 'backend/Valora.Web/wwwroot/img/brand/valora-logo-full.jpeg';
const symbol = 'backend/Valora.Web/wwwroot/img/brand/valora-symbol.jpeg';
for (const asset of [logo, symbol]) {
  if (!exists(asset)) {
    const msg = `${asset} não encontrado. PENDÊNCIA MANUAL: adicionar o binário oficial em backend/Valora.Web/wwwroot/img/brand`;
    if (allowMissing) warn.push(msg); else fail.push(msg);
  }
}
const files = [
  'backend/Valora.Web/Views/Shared/Public/_PublicTopbar.cshtml',
  'backend/Valora.Web/Views/Shared/Public/_PublicFooter.cshtml',
  'backend/Valora.Web/Views/Shared/_Sidebar.cshtml',
  'backend/Valora.Web/Views/Shared/_Topbar.cshtml',
  'backend/Valora.Web/Views/Shared/_PublicLayout.cshtml',
  'backend/Valora.Web/Views/Shared/_AdminLayout.cshtml',
  'backend/Valora.Web/Views/Home/Index.cshtml',
  'backend/Valora.Web/Views/Certificates/Details.cshtml',
  'backend/Valora.Web/Views/Certificates/Validate.cshtml',
  'backend/Valora.Web/Views/Results/Public.cshtml',
  'backend/Valora.Web/Views/PublicPages/FreeDiagnostic.cshtml',
  'backend/Valora.Web/Views/Account/Login.cshtml',
  'backend/Valora.Web/wwwroot/css/valora-public.css',
  'backend/Valora.Web/wwwroot/css/valora-admin.css',
  'backend/database/postgresql/script_completo.sql',
  'backend/database/postgresql/script_completo.sql'
];
for (const file of files) {
  const text = read(file);
  ok(!/brand-mark"\s*>\s*VG|>\s*VG\s*<|brand-symbol"\s*>\s*V\s*</.test(text), `${file} usa VG/V como marca final`);
  ok(!/<img\b[^>]+src=["']https?:\/\//i.test(text), `${file} usa imagem externa como logo`);
  ok(!/-----BEGIN PRIVATE KEY-----|"private_key"\s*:|firebase-adminsdk|"client_email"\s*:|"type"\s*:\s*"service_account"/i.test(text), `${file}: possível service account ou secret`);
  ok(!/<img\b[^>]+src=[\"'](?!\/img\/brand\/valora-(?:logo-full|symbol)\.jpeg|data:image\/svg\+xml|\/)/i.test(text), `${file}: path inseguro de imagem`);
  if (/valora-(logo-full|symbol)\.(png|svg|webp|jpg)/i.test(text)) fail.push(`${file}: referência de marca fora do JPEG oficial`);
}
ok(read('backend/Valora.Web/Views/Shared/Public/_PublicTopbar.cshtml').includes('brand-fallback-text'), 'Topbar pública não possui fallback textual');
ok(read('backend/Valora.Web/Views/Shared/_Sidebar.cshtml').includes('brand-fallback-text'), 'Sidebar admin não possui fallback textual');
ok(read('backend/Valora.Web/Views/Shared/_Topbar.cshtml').includes('brand-fallback-text'), 'Topbar admin não possui fallback textual');
ok(read('backend/Valora.Web/Views/Home/Index.cshtml').includes('/img/brand/valora-logo-full.jpeg'), 'Home não usa path oficial da logo completa');
ok(read('backend/Valora.Web/Views/Results/Public.cshtml').includes('/img/brand/valora-symbol.jpeg'), 'Resultado não usa símbolo oficial');
ok(read('backend/Valora.Web/Views/Certificates/Details.cshtml').includes('/img/brand/valora-logo-full.jpeg'), 'Certificado não usa logo completa');
ok(read('backend/database/postgresql/script_completo.sql').includes('/img/brand/valora-logo-full.jpeg') || read('backend/database/postgresql/script_completo.sql').includes('/img/brand/valora-logo-full.jpeg'), 'SQL não contém path da logo completa');
ok(read('backend/database/postgresql/script_completo.sql').includes('/img/brand/valora-symbol.jpeg') || read('backend/database/postgresql/script_completo.sql').includes('/img/brand/valora-symbol.jpeg'), 'SQL não contém path do símbolo');
if (warn.length) console.warn('validate-valora-brand-assets: WARN\n' + warn.join('\n'));
if (fail.length) { console.error('Validação de branding Valora falhou:\n' + fail.join('\n')); process.exit(1); }
console.log('validate-valora-brand-assets: PASS' + (allowMissing ? ' (diagnóstico)' : ''));

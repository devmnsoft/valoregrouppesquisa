const fs = require('fs');
const path = require('path');
const root = process.cwd();
const fail = [];
const warn = [];
const read = (file) => fs.existsSync(path.join(root, file)) ? fs.readFileSync(path.join(root, file), 'utf8') : '';
const exists = (file) => fs.existsSync(path.join(root, file));
const ok = (condition, message) => { if (!condition) fail.push(message); };
const allowMissing = String(process.env.VALORA_ALLOW_MISSING_BRAND_ASSETS || '').toLowerCase() === 'true';
const logo = 'backend/Valora.Web/wwwroot/img/brand/valora-logo-full.jpeg';
const symbol = 'backend/Valora.Web/wwwroot/img/brand/valora-symbol.jpeg';
if (!exists(logo) || !exists(symbol)) {
  const msg = 'PENDÊNCIA MANUAL: adicionar valora-logo-full.jpeg e valora-symbol.jpeg em backend/Valora.Web/wwwroot/img/brand';
  if (allowMissing) warn.push(msg); else { if (!exists(logo)) fail.push(`${logo} não encontrado`); if (!exists(symbol)) fail.push(`${symbol} não encontrado`); }
}
const files = [
  'backend/Valora.Web/Views/Shared/Public/_PublicTopbar.cshtml','backend/Valora.Web/Views/Shared/Public/_PublicFooter.cshtml','backend/Valora.Web/Views/Shared/_Sidebar.cshtml','backend/Valora.Web/Views/Shared/_Topbar.cshtml','backend/Valora.Web/Views/Shared/_PublicLayout.cshtml','backend/Valora.Web/Views/Home/Index.cshtml','backend/Valora.Web/Views/Certificates/Details.cshtml','backend/Valora.Web/Views/Certificates/Validate.cshtml','backend/Valora.Web/Views/Results/Public.cshtml','backend/Valora.Web/Views/PublicPages/FreeDiagnostic.cshtml','backend/Valora.Web/Views/Account/Login.cshtml','backend/Valora.Web/wwwroot/css/valora-public.css'
];
for (const file of files) {
  const text = read(file);
  ok(!/brand-mark"\s*>\s*VG|>\s*VG\s*<|brand-symbol"\s*>\s*V\s*</.test(text), `${file} usa VG/V como marca final`);
  const externalLogo = /<img\b[^>]+src=["']https?:\/\//i.test(text) && /logo|brand|valora/i.test(text);
  ok(!externalLogo, `${file} usa imagem externa como logo`);
  ok(!/-----BEGIN PRIVATE KEY-----|"private_key"\s*:|firebase-adminsdk|"client_email"\s*:|"type"\s*:\s*"service_account"/i.test(text), `${file}: possível service account ou secret`);
}
ok(read('backend/Valora.Web/Views/Shared/Public/_PublicTopbar.cshtml').includes('brand-fallback-text'), 'Topbar pública não possui fallback textual');
ok(read('backend/Valora.Web/Views/Shared/_Sidebar.cshtml').includes('brand-fallback-text'), 'Sidebar admin não possui fallback textual');
ok(read('backend/Valora.Web/Views/Home/Index.cshtml').includes('/img/brand/valora-logo-full.jpeg'), 'Home não usa path oficial da logo completa');
if (warn.length) console.warn(warn.join('\n'));
if (fail.length) { console.error('Validação de branding Valora falhou:\n' + fail.join('\n')); process.exit(1); }
console.log('validate-valora-brand-assets: PASS');

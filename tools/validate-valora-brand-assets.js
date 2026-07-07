const fs = require('fs');
const path = require('path');

const root = process.cwd();
const fail = [];
const read = (file) => fs.existsSync(path.join(root, file)) ? fs.readFileSync(path.join(root, file), 'utf8') : '';
const exists = (file) => fs.existsSync(path.join(root, file));
const ok = (condition, message) => { if (!condition) fail.push(message); };

const logo = 'backend/Valora.Web/wwwroot/img/brand/valora-logo-full.jpeg';
const symbol = 'backend/Valora.Web/wwwroot/img/brand/valora-symbol.jpeg';
const publicTopbar = 'backend/Valora.Web/Views/Shared/Public/_PublicTopbar.cshtml';
const sidebar = 'backend/Valora.Web/Views/Shared/_Sidebar.cshtml';
const home = 'backend/Valora.Web/Views/Home/Index.cshtml';
const layout = 'backend/Valora.Web/Views/Shared/_PublicLayout.cshtml';
const sqlFiles = ['scriptbd_completo.sql', 'database/postgresql/scriptbd_completo.sql', ...fs.readdirSync(path.join(root, 'database/postgresql')).filter(f => f.endsWith('.sql')).map(f => `database/postgresql/${f}`)];

ok(exists(logo), `${logo} não encontrado`);
ok(exists(symbol), `${symbol} não encontrado`);
ok(!/brand-mark"\s*>\s*VG|>\s*VG\s*</.test(read(publicTopbar)), '_PublicTopbar.cshtml ainda usa VG textual');
ok(!/brand-symbol"\s*>\s*V\s*</.test(read(sidebar)), '_Sidebar.cshtml ainda usa V textual em brand-symbol');
ok(read(home).includes('/img/brand/valora-logo-full.jpeg'), 'Home não usa valora-logo-full.jpeg');
ok(read(layout).includes('<link rel="icon" href="/img/brand/valora-symbol.jpeg">'), 'Layout público não aponta favicon para o símbolo');
ok(read(layout).includes('<link rel="apple-touch-icon" href="/img/brand/valora-symbol.jpeg">'), 'Layout público não aponta apple touch icon para o símbolo');

const sql = sqlFiles.filter(exists).map(read).join('\n');
ok(sql.includes("/img/brand/valora-logo-full.jpeg"), 'SQL não contém caminho da logo completa');
ok(sql.includes("/img/brand/valora-symbol.jpeg"), 'SQL não contém caminho do símbolo');
ok(sql.includes("#0c3448"), 'SQL não contém primary_color oficial');
ok(sql.includes("#75dce8"), 'SQL não contém secondary_color oficial');
ok(/ADD COLUMN IF NOT EXISTS symbol_url|symbol_url\s+text/i.test(sql), 'SQL não adiciona/declara symbol_url de forma idempotente');

const webFiles = [
  publicTopbar, sidebar, home, layout,
  'backend/Valora.Web/Views/Shared/_Topbar.cshtml',
  'backend/Valora.Web/Views/Certificates/Details.cshtml',
  'backend/Valora.Web/Views/Certificates/Validate.cshtml',
  'backend/Valora.Web/Views/Results/Public.cshtml'
];
for (const file of webFiles) {
  const text = read(file);
  const externalLogo = /<img\b[^>]+src=["']https?:\/\//i.test(text) && /logo|brand|valora/i.test(text);
  ok(!externalLogo, `${file} usa imagem externa como logo`);
}

const jsonFiles = ['backend', 'database', 'tools', 'scripts']
  .flatMap((dir) => {
    function walk(current) {
      const full = path.join(root, current);
      if (!fs.existsSync(full)) return [];
      return fs.readdirSync(full, { withFileTypes: true }).flatMap((entry) => {
        if (['node_modules', 'bin', 'obj', '.git'].includes(entry.name)) return [];
        const rel = path.join(current, entry.name);
        return entry.isDirectory() ? walk(rel) : [rel];
      });
    }
    return walk(dir);
  })
  .filter((file) => file.endsWith('.json'));
for (const file of jsonFiles) {
  const text = read(file);
  if (/-----BEGIN PRIVATE KEY-----|"private_key"\s*:|firebase-adminsdk|"client_email"\s*:|"type"\s*:\s*"service_account"/i.test(text)) {
    fail.push(`${file}: possível JSON de service account ou segredo versionado`);
  }
}

if (fail.length) {
  console.error('Validação de branding Valora falhou:\n' + fail.join('\n'));
  process.exit(1);
}
console.log('validate-valora-brand-assets: PASS');

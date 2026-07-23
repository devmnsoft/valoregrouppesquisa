#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

let failed = false;
function ok(condition, message) {
  if (condition) console.log(`OK ${message}`);
  else { console.error(`FAIL ${message}`); failed = true; }
}
function read(file) { return fs.existsSync(file) ? fs.readFileSync(file, 'utf8') : ''; }
function walk(dir, predicate = () => true) {
  if (!fs.existsSync(dir)) return [];
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (['node_modules', 'bin', 'obj', '.git'].includes(entry.name)) continue;
      out.push(...walk(full, predicate));
    } else if (predicate(full)) out.push(full);
  }
  return out;
}

const requiredDocs = [
  'SPRINT_BACKEND_OFICIAL_RC2_REAL_HOMOLOGATION_DIAGNOSTIC.md',
  'LEGACY_TO_BACKEND_PARITY_FINAL_REVIEW.md',
  'HOMOLOGATION_BUG_REPORT.md',
  'SPRINT_BACKEND_OFICIAL_RC2_REAL_HOMOLOGATION_AUDIT.md'
];
requiredDocs.forEach(file => ok(fs.existsSync(file), `${file} existe`));

const pkg = JSON.parse(read('package.json') || '{}');
ok(Boolean(pkg.scripts && pkg.scripts['backend:sql-schema-validate']), 'backend:sql-schema-validate está no package.json');
ok(Boolean(pkg.scripts && pkg.scripts['backend:rc2-homologation-validate']), 'backend:rc2-homologation-validate está no package.json');

const sqlFiles = ['scriptbd_completo.sql', ...walk('database/postgresql', f => f.endsWith('.sql'))];
const officialSql = sqlFiles.map(f => read(f)).join('\n');
ok(!/\bprice_label\b/i.test(officialSql), 'price_label não aparece no SQL oficial');
ok(!/\bbadge\b/i.test(officialSql), 'badge não aparece no SQL oficial');
ok(!/\bpublic_subtitle\b|\bpublic_description\b|\bhighlight_text\b|\bcta_label\b/i.test(officialSql), 'colunas públicas legadas não aparecem no SQL oficial');
ok(/\blimit_key\b[\s\S]*\blimit_value\b/i.test(officialSql) && /\bcapability_key\b/i.test(officialSql), 'colunas canonicas de limits/capabilities aparecem no SQL oficial');

const sln = read('backend/Valora.sln');
ok(!/backend-v2/i.test(sln), 'backend-v2 não é build oficial da solution');

const webFiles = walk('backend/Valora.Web', f => /\.(cs|cshtml|js|json)$/.test(f));
const webText = webFiles.map(f => read(f)).join('\n');
ok(!/firebase/i.test(webText), 'Web oficial não usa Firebase');
ok(!/Npgsql|Dapper|PostgresConnectionFactory|SELECT\s+.+\s+FROM\s+valorapesquisa/i.test(webText), 'Web oficial não acessa banco diretamente');

ok(fs.existsSync('tools/linux/backend-prd-01-build-release.sh') && fs.existsSync('tools/linux/backend-prd-04-package-release.sh'), 'scripts Linux de release existem');
ok(fs.existsSync('tools/windows/backend-prd-01-build-release.bat') && fs.existsSync('tools/windows/backend-prd-04-package-release.bat'), 'scripts Windows de release existem');

const packageDirs = ['release', 'releases', 'dist', 'artifacts'].filter(fs.existsSync);
const packagedFiles = packageDirs.flatMap(d => walk(d));
ok(!packagedFiles.some(f => path.basename(f) === '.env'), 'pacotes não incluem .env');
ok(!packagedFiles.some(f => /\.(dump|bak|backup)$|dump/i.test(f)), 'pacotes não incluem dump');

const releaseNotes = read('RELEASE_CANDIDATE_NOTES.md');
ok(/0\.9\.0-rc2/.test(releaseNotes), 'release notes apontam para 0.9.0-rc2');

if (failed) process.exit(1);
console.log('validate-backend-official-rc2-homologation: PASS');

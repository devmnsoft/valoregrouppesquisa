#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const cp = require('child_process');

const root = path.resolve(__dirname, '..');
const dist = path.join(root, 'dist');
const publishDir = path.join(root, 'publish');
const reportsDir = path.join(publishDir, 'reports');
const backupRoot = path.join(root, 'backups', 'iis');
const webConfigTemplate = path.join(root, 'templates', 'iis', 'web.config');
const stamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 12);
const report = { errors: [], checks: [], js: [], css: [], backup: '', packagePath: '', iisValidation: null, distValidation: null, dataImported: false, healthcheck: null };

function parseArgs(argv) {
  const out = { mode: 'firebase', backup: true };
  for (let i = 0; i < argv.length; i += 1) {
    const a = argv[i];
    if (!a.startsWith('--')) continue;
    const key = a.slice(2);
    if (['dry-run', 'apply', 'package-only', 'with-data', 'backup', 'skip-security-check', 'skip-build'].includes(key)) out[key.replace(/-([a-z])/g, (_, c) => c.toUpperCase())] = true;
    else if (key === 'no-backup') out.backup = false;
    else out[key.replace(/-([a-z])/g, (_, c) => c.toUpperCase())] = argv[++i];
  }
  if (!out.apply && !out.packageOnly) out.dryRun = true;
  return out;
}
const args = parseArgs(process.argv.slice(2));
function fail(msg) { report.errors.push(msg); throw new Error(msg); }
function log(msg) { console.log(msg); }
function run(cmd, opts = {}) { log(`$ ${cmd}`); cp.execSync(cmd, { cwd: root, stdio: 'inherit', shell: true, ...opts }); }
function runHealthcheck() {
  if (!args.healthUrl) return;
  if (!args.project) fail('--health-url exige --project para validar Firebase PRD.');
  const cmd = `node scripts/healthcheck-prd.js --url "${args.healthUrl}" --project "${args.project}" --check-firebase --check-functions`;
  try {
    run(cmd);
    report.healthcheck = 'OK';
  } catch (err) {
    report.healthcheck = 'FALHA';
    fail(`Health check PRD falhou após a publicação. Backup disponível em ${report.backup || 'não criado'}. Sugestão: executar rollback antes de liberar o ambiente.`);
  }
}
function ensureFile(rel) { const p = path.join(root, rel); if (!fs.existsSync(p)) fail(`Arquivo obrigatório não encontrado: ${rel}`); return p; }
function readConfig() { ensureFile('config.js'); const txt = fs.readFileSync(path.join(root, 'config.js'), 'utf8'); return { txt, storage: /STORAGE_MODE\s*:\s*['"]([^'"]+)/.exec(txt)?.[1], firebaseEnabled: /FIREBASE_ENABLED\s*:\s*(true|false)/.exec(txt)?.[1] === 'true', hasFirebaseConfig: ['apiKey', 'authDomain', 'projectId', 'appId'].every(k => new RegExp(`${k}\\s*:\\s*['\"][^'\"]+`).test(txt)) }; }
function prevalidate() { ensureFile('index.html'); ensureFile('package.json'); ensureFile('scripts/build-production.js'); ensureFile('templates/iis/web.config'); if (args.apply && !args.iisPath) fail('--iis-path é obrigatório com --apply.'); if (args.apply && !fs.existsSync(args.iisPath)) fs.mkdirSync(args.iisPath, { recursive: true }); }
function validateMode() { const c = readConfig(); if (args.mode === 'local') { if (c.storage !== 'local' || c.firebaseEnabled) fail('Modo local exige STORAGE_MODE="local" e FIREBASE_ENABLED=false em config.js.'); } else if (args.mode === 'firebase') { if (c.storage !== 'firebase' || !c.firebaseEnabled) fail('Modo firebase exige STORAGE_MODE="firebase" e FIREBASE_ENABLED=true em config.js.'); if (!c.hasFirebaseConfig) fail('Firebase PRD está habilitado, mas FIREBASE_CONFIG está vazio. Preencha config.js antes de publicar.'); } else fail('--mode deve ser local ou firebase.'); }
function walk(dir) { if (!fs.existsSync(dir)) return []; return fs.readdirSync(dir, { withFileTypes: true }).flatMap(d => { const p = path.join(dir, d.name); return d.isDirectory() ? walk(p) : [p]; }); }
function assetRefs(html) { return [...html.matchAll(/(?:src|href)=["'](assets\/[^"']+\.(?:js|css))["']/g)].map(m => m[1]); }
function validateDist(dir = dist) { const index = path.join(dir, 'index.html'), assets = path.join(dir, 'assets'); if (!fs.existsSync(index)) fail('Build inconsistente: dist/index.html não existe.'); if (!fs.existsSync(assets)) fail('Build inconsistente: dist/assets não existe.'); const files = walk(dir).map(f => path.relative(dir, f).replace(/\\/g, '/')); const js = files.filter(f => /^assets\/app\..+\.js$/.test(f)); const css = files.filter(f => /^assets\/style\..+\.css$/.test(f)); if (!js.length) fail('Build inconsistente: nenhum assets/app.*.js encontrado.'); if (!css.length) fail('Build inconsistente: nenhum assets/style.*.css encontrado.'); const forbidden = files.filter(f => /(^|\/)(\.env|.*serviceAccount.*\.json|exports|backups)(\/|$)|\.map$|token.*telegram|smtp.*password/i.test(f)); if (forbidden.length) fail(`Build bloqueado: arquivos proibidos em dist: ${forbidden.join(', ')}`); const html = fs.readFileSync(index, 'utf8'); for (const ref of assetRefs(html)) if (!fs.existsSync(path.join(dir, ref))) fail(`Build inconsistente: index.html aponta para ${ref}, mas o arquivo não existe.`); report.js = js; report.css = css; report.distValidation = 'OK'; return { js, css, files }; }
function copyDirectory(src, dest) { fs.mkdirSync(dest, { recursive: true }); for (const e of fs.readdirSync(src, { withFileTypes: true })) { const s = path.join(src, e.name), d = path.join(dest, e.name); if (e.isDirectory()) copyDirectory(s, d); else fs.copyFileSync(s, d); } }
function emptyDirectory(dir) { fs.mkdirSync(dir, { recursive: true }); for (const e of fs.readdirSync(dir)) fs.rmSync(path.join(dir, e), { recursive: true, force: true }); }
function backupDirectory(src) { const name = `valoragroup-${stamp}`; const dest = path.join(backupRoot, name); if (fs.existsSync(src) && fs.readdirSync(src).length) copyDirectory(src, dest); else fs.mkdirSync(dest, { recursive: true }); report.backup = path.relative(root, dest); return dest; }
function validateIisPublication(iisPath) { validateDist(iisPath); const files = walk(iisPath).map(f => path.relative(iisPath, f).replace(/\\/g, '/')); for (const f of ['index.html', 'web.config']) if (!files.includes(f)) fail(`Publicação IIS inválida: ${f} ausente.`); const loose = files.filter(f => /^(app|config|firebase-repository|local-repository|repository)\.js$/.test(f) || /^(exports|backups)(\/|$)/.test(f)); if (loose.length) fail(`Publicação IIS inválida: arquivos proibidos: ${loose.join(', ')}`); report.iisValidation = 'OK'; }
function generateWebConfig() { fs.copyFileSync(webConfigTemplate, path.join(dist, 'web.config')); }
function packageOnly() { const dest = path.join(publishDir, `valoragroup-iis-prd-${stamp}`); fs.rmSync(dest, { recursive: true, force: true }); copyDirectory(dist, dest); report.packagePath = path.relative(root, dest); log(`Pacote gerado em ${report.packagePath}`); }
function writeReport(status) { fs.mkdirSync(reportsDir, { recursive: true }); const body = `# Relatório de publicação IIS PRD\n\n- Data: ${new Date().toISOString()}\n- Modo: ${args.mode}\n- Projeto Firebase: ${args.project || 'não informado'}\n- Caminho IIS: ${args.iisPath || 'não aplicado'}\n- Build version: ${readConfig().txt.match(/APP_VERSION:\s*'([^']+)'/)?.[1] || 'n/a'}\n- JS gerados: ${report.js.join(', ') || 'n/a'}\n- CSS gerados: ${report.css.join(', ') || 'n/a'}\n- web.config usado: templates/iis/web.config\n- Backup criado: ${report.backup || 'não'}\n- Dados importados: ${report.dataImported ? 'sim' : 'não'}\n- Validação dist: ${report.distValidation || 'não executada'}\n- Validação IIS: ${report.iisValidation || 'não executada'}\n- Health check PRD: ${report.healthcheck || 'não executado'}\n- Erros: ${report.errors.join('; ') || 'nenhum'}\n- Status final: ${status}\n`;
  const file = path.join(reportsDir, `iis-prd-publish-${stamp}.md`); fs.writeFileSync(file, body, 'utf8'); log(`Relatório: ${path.relative(root, file)}`); }
try {
  prevalidate(); validateMode();
  run('npm run check'); if (!args.skipSecurityCheck) run('npm run security:check');
  if (args.withData) { if (!args.dataFile || !args.project) fail('--with-data exige --data-file e --project.'); run(`node scripts/import-local-export-to-firebase.js --file "${args.dataFile}" --project "${args.project}" --dry-run`); if (args.apply) { run(`node scripts/import-local-export-to-firebase.js --file "${args.dataFile}" --project "${args.project}" --apply --backup`); report.dataImported = true; } }
  if (!args.skipBuild) run('npm run build:prod');
  generateWebConfig(); validateDist();
  if (args.packageOnly) packageOnly();
  else if (args.apply) { if (args.backup) backupDirectory(args.iisPath); emptyDirectory(args.iisPath); copyDirectory(dist, args.iisPath); validateIisPublication(args.iisPath); runHealthcheck(); log('Publicação IIS concluída.'); }
  else log('Dry-run concluído. Nada foi copiado para o IIS.');
  writeReport('SUCESSO');
} catch (err) { console.error(`\nERRO: ${err.message}`); writeReport('FALHA'); process.exit(1); }

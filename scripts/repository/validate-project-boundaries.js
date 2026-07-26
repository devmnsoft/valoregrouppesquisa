#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const cp = require('child_process');
const root = process.cwd();
const skip = new Set(['.git','node_modules','functions/node_modules','communication-gateway/node_modules','dist','bin','obj']);
function walk(dir, out=[]) {
  const rel = path.relative(root, dir).replace(/\\/g,'/');
  if (skip.has(rel) || [...skip].some(s => rel.startsWith(s + '/'))) return out;
  for (const entry of fs.readdirSync(dir, {withFileTypes:true})) {
    const full = path.join(dir, entry.name);
    const r = path.relative(root, full).replace(/\\/g,'/');
    if ([...skip].some(s => r === s || r.startsWith(s + '/'))) continue;
    if (entry.isDirectory()) walk(full, out); else out.push(r);
  }
  return out;
}
const files = walk(root);
const errors = [];
const sln = files.filter(f => f.endsWith('.sln'));
if (sln.length !== 1 || sln[0] !== 'backend/Valora.sln') errors.push(`Solutions inválidas: ${sln.join(', ') || '(nenhuma)'}`);
for (const f of files.filter(f => f.endsWith('.csproj') && !f.startsWith('backend/'))) errors.push(`.csproj fora de backend/: ${f}`);
for (const d of ['backend'+'-v2','backend'+'-v3','src/'+'Habit'+'Flow']) if (fs.existsSync(path.join(root,d))) errors.push(`Diretório proibido: ${d}`);
for (const f of files.filter(f => f === 'global.json')) errors.push('global.json deve estar em backend/global.json');
if (!fs.existsSync(path.join(root,'backend/global.json'))) errors.push('backend/global.json ausente');
if (fs.existsSync(path.join(root,'database'))) errors.push('database/ raiz não deve existir');
for (const f of files.filter(f => /^(ASPNET_|BACKEND_|NOVO_PROJETO_DOTNET_|SPRINT_.*(DOTNET|BACKEND)|HOMOLOGACAO_CUTOVER_CHECKLIST|CUTOVER_PLAN|ROLLBACK_PLAN|BACKUP_RESTORE_RUNBOOK|LEGACY_RETIREMENT_PLAN|RELEASE_CANDIDATE_NOTES|REQUISITO_MIGRACAO_COMPLETA_ASPNET_CORE_10|SAAS_FINAL_ACCEPTANCE_CHECKLIST|SECURITY_HARDENING_CHECKLIST|BANCO_COMPLETO_GUIA_EXECUCAO).*\.md$/i.test(f))) errors.push(`Documento .NET solto na raiz: ${f}`);
for (const f of files.filter(f => f.startsWith('tools/') && /backend|postgres|aspnet|dotnet|migration/i.test(path.basename(f)))) errors.push(`Validador backend solto em tools/: ${f}`);
const textFiles = files.filter(f => !/\.(png|jpg|jpeg|gif|ico|pdf|zip|gz|woff2?|ttf|eot)$/i.test(f));
for (const f of textFiles) {
  let t; try { t = fs.readFileSync(path.join(root,f),'utf8'); } catch { continue; }
  const forbiddenProduct = 'habit' + 'flow';
  if (new RegExp(forbiddenProduct, 'i').test(t)) errors.push(`Texto proibido produto externo em ${f}`);
  if (new RegExp('schema\\s+' + forbiddenProduct, 'i').test(t)) errors.push(`Schema proibido produto externo em ${f}`);
  if (new RegExp('Valora'+'Pesquisa\\.sln|src/'+'Habit'+'Flow|50'+'97').test(t)) errors.push(`Referência proibida em ${f}`);
  if (f.startsWith('backend/') && /firebase/i.test(t) && !/README|docs|Tests|\.csproj|appsettings|database|tools/.test(f)) errors.push(`Referência Firebase em runtime backend: ${f}`);
}
if (errors.length) { console.error(errors.join('\n')); process.exit(1); }
console.log('Repository boundaries OK');

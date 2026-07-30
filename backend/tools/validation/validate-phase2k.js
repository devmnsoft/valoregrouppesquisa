#!/usr/bin/env node
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const failures = [];
const read = p => fs.readFileSync(path.join(root, p), 'utf8');
const walk = dir => fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(dir, e.name)) : [path.join(dir, e.name)]);
const repositories = walk(path.join(root, 'Valora.Infrastructure/Repositories')).filter(x => x.endsWith('.cs'));
for (const file of repositories) {
  const source = fs.readFileSync(file, 'utf8');
  if (/MigrationImportStore|ConcurrentDictionary|Task\.FromResult/.test(source)) failures.push(`${path.relative(root, file)} contains simulated persistence`);
  if (/_\s*=\s*new CommandDefinition/.test(source)) failures.push(`${path.relative(root, file)} discards a SQL command`);
}
const sql = read('database/postgresql/script_completo.sql');
const withoutComments = sql.replace(/--.*$/gm, '').replace(/\/\*[\s\S]*?\*\//g, '');
const transactions = { begin: (withoutComments.match(/^\s*BEGIN\s*;/gmi) || []).length, commit: (withoutComments.match(/^\s*COMMIT\s*;/gmi) || []).length };
if (transactions.begin !== 1 || transactions.commit !== 1) failures.push(`expected one transaction, got ${transactions.begin} BEGIN and ${transactions.commit} COMMIT`);
for (const table of ['migration_batches','migration_source_files','migration_records','migration_mappings','migration_conflicts','migration_checkpoints','rollback_records']) {
  if (!new RegExp(`CREATE TABLE IF NOT EXISTS valorapesquisa\\.${table}\\b`, 'i').test(sql)) failures.push(`missing qualified migration table ${table}`);
}
const report = { phase: '2K', generatedAt: new Date().toISOString(), transactions, passed: failures.length === 0, failures };
fs.mkdirSync(path.join(root, 'artifacts'), { recursive: true });
fs.writeFileSync(path.join(root, 'artifacts/phase2k-validation.json'), JSON.stringify(report, null, 2) + '\n');
if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log('Phase 2K persistence and atomic SQL validation passed.');

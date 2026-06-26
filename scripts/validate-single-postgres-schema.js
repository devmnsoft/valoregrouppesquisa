#!/usr/bin/env node
'use strict';
const fs = require('fs');
const path = require('path');

const roots = ['database/postgresql', 'backend', 'migration', 'scripts'];
const forbiddenSchemas = ['valora', 'billing', 'communication', 'audit', 'migration'];
const forbiddenSchemaRef = new RegExp(`\\b(${forbiddenSchemas.filter(name => name !== 'audit' && name !== 'migration').join('|')})\\.`);
const forbiddenCreate = new RegExp(`CREATE\\s+SCHEMA\\s+IF\\s+NOT\\s+EXISTS\\s+(${forbiddenSchemas.join('|')})\\b`, 'i');
const allowed = 'valorapesquisa.';
const errors = [];

function listFiles(directory) {
  if (!fs.existsSync(directory)) return [];
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const filePath = path.join(directory, entry.name);
    if (entry.isDirectory()) return listFiles(filePath);
    return [filePath];
  });
}

const files = roots.flatMap(listFiles).filter(file => /\.(cs|sql|js|json|md|bat)$/i.test(file));
for (const file of files) {
  const text = fs.readFileSync(file, 'utf8');
  const shouldCheckSchemaRef = file.endsWith('.sql') || file.includes('Repositories') || file.includes('Database') || file.startsWith('migration' + path.sep);
  if ((shouldCheckSchemaRef && forbiddenSchemaRef.test(text)) || forbiddenCreate.test(text) || text.includes('valorapesquisa' + '.LogAsync')) {
    errors.push(`${file}: schema legado proibido`);
  }
}

const rootSql = listFiles('database/postgresql')
  .filter(file => path.dirname(file) === 'database/postgresql' && file.endsWith('.sql'));
if (!rootSql.some(file => fs.readFileSync(file, 'utf8').includes(allowed))) {
  errors.push('nenhum SQL canônico usa valorapesquisa.');
}

if (errors.length) {
  console.error(errors.join('\n'));
  process.exit(1);
}
console.log('validate-single-postgres-schema: PASS');

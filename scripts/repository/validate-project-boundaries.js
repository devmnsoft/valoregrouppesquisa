#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const boundaries = require('./product-boundaries.json');

const ignoredRoots = new Set([
  '.git', 'node_modules', 'functions/node_modules',
  'communication-gateway/node_modules', 'dist', 'bin', 'obj'
]);
const classifierSources = new Set([
  'scripts/repository/product-boundaries.json',
  'scripts/repository/validate-project-boundaries.js',
  'scripts/repository/validate-project-boundaries.test.js'
]);

function normalize(value) {
  return value.replace(/\\/g, '/');
}

function isIgnored(relativePath) {
  return [...ignoredRoots].some(root => relativePath === root || relativePath.startsWith(`${root}/`));
}

function walk(root, directory = root, output = []) {
  const directoryRelative = normalize(path.relative(root, directory));
  if (isIgnored(directoryRelative)) return output;
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    const relativePath = normalize(path.relative(root, fullPath));
    if (isIgnored(relativePath)) continue;
    if (entry.isDirectory()) walk(root, fullPath, output);
    else output.push(relativePath);
  }
  return output;
}

function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function classifyFile(relativePath, content = '') {
  const violations = [];
  const pathParts = relativePath.split('/');
  const forbiddenPattern = new RegExp(boundaries.forbiddenProductNames.map(escapeRegExp).join('|'), 'i');
  if (forbiddenPattern.test(relativePath)) violations.push('caminho de produto externo');
  if (!classifierSources.has(relativePath) && forbiddenPattern.test(content)) {
    violations.push('conteúdo de produto externo');
  }
  if (relativePath.endsWith('.sln') && relativePath !== boundaries.officialSolution) {
    violations.push('solution não oficial');
  }
  if (relativePath.endsWith('.csproj') && !relativePath.startsWith(`${boundaries.officialBackendRoot}/`)) {
    violations.push('projeto .NET fora do backend oficial');
  }
  if (/^backend-v\d*(?:\/|$)/i.test(relativePath)) violations.push('árvore de backend paralela');
  if (relativePath === 'backend/database/postgresql/banco_completo.sql') violations.push('script SQL legado');
  if (relativePath.startsWith('backend/database/postgresql/migrations/')) violations.push('migration PostgreSQL ativa');
  if (pathParts.some(part => /^backend-v\d+$/i.test(part)) || relativePath.startsWith('src/')) {
    violations.push('árvore de backend paralela');
  }
  if (/\bmigration\b/i.test(relativePath) && !relativePath.startsWith(`${boundaries.officialBackendRoot}/`) && /\.sql$/i.test(relativePath)) {
    violations.push('migration fora do backend oficial');
  }
  if (/\b(requirement|requisito)\b/i.test(relativePath) && forbiddenPattern.test(content)) {
    violations.push('requisito incompatível');
  }
  const externalFileReference = content.match(/(?:src|database|docs)\/[\w./-]+/gi) || [];
  if (externalFileReference.some(reference => forbiddenPattern.test(reference))) {
    violations.push('referência de arquivo de produto externo');
  }
  return [...new Set(violations)];
}

function validateRepository(root = process.cwd()) {
  const files = walk(root);
  const violations = [];
  const solutions = files.filter(file => file.endsWith('.sln'));
  if (solutions.length !== 1 || solutions[0] !== boundaries.officialSolution) {
    violations.push(`Solutions inválidas: ${solutions.join(', ') || '(nenhuma)'}`);
  }
  if (!fs.existsSync(path.join(root, 'backend/global.json'))) violations.push('backend/global.json ausente');
  if (fs.existsSync(path.join(root, 'database'))) violations.push('database/ raiz não deve existir');
  const databaseSql = files.filter(file => file.startsWith('backend/database/postgresql/') && file.endsWith('.sql'));
  if (databaseSql.length !== 1 || databaseSql[0] !== boundaries.officialDatabaseFile) {
    violations.push(`Scripts SQL oficiais inválidos: ${databaseSql.join(', ') || '(nenhum)'}`);
  }

  const binaryPattern = /\.(png|jpg|jpeg|gif|ico|pdf|zip|gz|woff2?|ttf|eot)$/i;
  for (const file of files) {
    let content = '';
    if (!binaryPattern.test(file)) {
      try { content = fs.readFileSync(path.join(root, file), 'utf8'); } catch { continue; }
    }
    for (const reason of classifyFile(file, content)) violations.push(`${reason}: ${file}`);
    if (file === 'global.json') violations.push('global.json deve estar em backend/global.json');
    if (file.startsWith('backend/') && /firebase/i.test(content) && !/README|docs|Tests|\.csproj|appsettings|database|tools/.test(file)) {
      violations.push(`Referência Firebase em runtime backend: ${file}`);
    }
  }
  return violations;
}

if (require.main === module) {
  const violations = validateRepository();
  if (violations.length) {
    console.error(violations.join('\n'));
    process.exitCode = 1;
  } else {
    console.log('Repository boundaries OK');
  }
}

module.exports = { classifyFile, validateRepository };

#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const root = path.resolve(__dirname, '..');
const indexFile = path.join(root, 'index.js');

if (!fs.existsSync(indexFile) || !fs.statSync(indexFile).isFile()) {
  console.error('[functions:lint] Arquivo obrigatório não encontrado: index.js');
  process.exit(1);
}

const targets = [
  indexFile,
  path.join(root, 'utils')
];

function collectJsFiles(target) {
  if (!fs.existsSync(target)) return [];

  const stat = fs.statSync(target);

  if (stat.isFile() && target.endsWith('.js')) {
    return [target];
  }

  if (!stat.isDirectory()) return [];

  const files = [];

  for (const entry of fs.readdirSync(target, { withFileTypes: true })) {
    const full = path.join(target, entry.name);

    if (entry.isDirectory()) {
      files.push(...collectJsFiles(full));
    } else if (entry.isFile() && entry.name.endsWith('.js')) {
      files.push(full);
    }
  }

  return files;
}

const files = targets.flatMap(collectJsFiles);

if (!files.length) {
  console.error('Nenhum arquivo JS encontrado para validação das Functions.');
  process.exit(1);
}

for (const file of files) {
  const relative = path.relative(root, file);
  console.log(`[functions:lint] node --check ${relative}`);

  const result = spawnSync(process.execPath, ['--check', file], {
    cwd: root,
    stdio: 'inherit',
    shell: false
  });

  if (result.status !== 0) {
    console.error(`[functions:lint] Falha de sintaxe em ${relative}`);
    process.exit(result.status || 1);
  }
}

console.log(`[functions:lint] PASS (${files.length} arquivo(s) validado(s)).`);

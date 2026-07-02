#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const validatorPath = path.join(root, 'scripts', 'validate-dist-action-handlers.js');
const src = fs.readFileSync(validatorPath, 'utf8');
const failures = [];

function assert(condition, message) {
  if (!condition) failures.push(message);
}

assert(/function\s+normalizeRelPath\s*\(/.test(src), 'validate-dist-action-handlers.js deve possuir normalizeRelPath.');
assert(src.includes(".replace(/\\\\/g, '/')"), "normalizeRelPath deve usar replace(/\\\\/g, '/').");
assert(/walk\(dist\)[\s\S]*\.map\(\(?\s*(?:f|file)\s*\)?\s*=>\s*normalizeRelPath\(\s*(?:f|file)\s*\)\)/.test(src), 'Arquivos de dist devem ser normalizados antes do filtro.');
assert(/\(\^\|\\\/\)app\\\.\[a-f0-9\]\{8,\}\\\.js\$/.test(src), 'Validador deve procurar app.<hash>.js em subpastas.');
assert(/allJs[\s\S]*JS encontrados/.test(src), 'Mensagem de erro deve listar os arquivos JS encontrados.');
assert(/Bundle dist\/assets\/app\*\.js não encontrado/.test(src), 'Mensagem deve citar dist/assets/app*.js.');
assert(!/Bundle dist\/app\*\.js não encontrado/.test(src), 'Mensagem não deve ficar fixa apenas em dist/app*.js.');
assert(!/walk\(dist\)\.filter\([^)]*\(\^\|\\\/\)app/.test(src), 'Filtro não deve aplicar regex em caminhos sem normalização.');

if (failures.length) {
  console.error('Validação do suporte a paths Windows falhou:');
  failures.forEach((failure) => console.error(`* ${failure}`));
  process.exit(1);
}

console.log('Validador de dist aceita paths Windows e bundles em subpastas.');

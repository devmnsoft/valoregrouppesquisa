#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const { validateApp } = require('./action-handler-utils');

const root = path.resolve(__dirname, '..');
const dist = path.join(root, 'dist');

function walk(dir, prefix = '') {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((d) => (
    d.isDirectory()
      ? walk(path.join(dir, d.name), path.join(prefix, d.name))
      : [path.join(prefix, d.name)]
  ));
}

function normalizeRelPath(file) {
  return String(file || '').replace(/\\/g, '/');
}

const allDistFiles = fs.existsSync(dist) ? walk(dist).map((f) => normalizeRelPath(f)) : [];
const files = allDistFiles.filter((f) => /(^|\/)app\.[a-f0-9]{8,}\.js$|(^|\/)app\.js$/.test(f));
const allJs = allDistFiles.filter((f) => f.endsWith('.js'));

const failures = [];
if (!files.length) {
  failures.push(
    `Bundle dist/assets/app*.js não encontrado. JS encontrados: ${allJs.slice(0, 20).join(', ') || '(nenhum)'}`,
  );
}

const newest = files.map((f) => ({
  f,
  t: fs.statSync(path.join(dist, f)).mtimeMs,
})).sort((a, b) => b.t - a.t)[0];

if (newest) {
  const src = fs.readFileSync(path.join(dist, newest.f), 'utf8');
  failures.push(...validateApp(src).failures.map((f) => `${newest.f}: ${f}`));
  if (!/ValoraBuildInfo/.test(src)) failures.push(`${newest.f}: window.ValoraBuildInfo não encontrado.`);
  if (/addDimension\s*,/.test(src) && !/function\s+addDimension\s*\(/.test(src)) failures.push(`${newest.f}: addDimension em createActions sem declaração óbvia.`);
  const old = files.filter((f) => f !== newest.f && fs.statSync(path.join(dist, f)).mtimeMs < newest.t - 1000);
  if (old.length > 3) failures.push(`Há bundles antigos em dist: ${old.slice(0, 5).join(', ')}`);
}

if (failures.length) {
  console.error('Validação de dist action handlers falhou:');
  failures.forEach((f) => console.error(`* ${f}`));
  process.exit(1);
}
console.log(`Dist validado: ${newest.f}`);

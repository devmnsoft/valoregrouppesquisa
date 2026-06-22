#!/usr/bin/env node
const { execFileSync } = require('child_process');

function git(args) {
  return execFileSync('git', args, { encoding: 'utf8' }).trim();
}

const changed = new Set();

for (const args of [
  ['diff', '--name-only', '--cached'],
  ['diff', '--name-only'],
]) {
  const output = git(args);
  if (output) output.split(/\r?\n/).filter(Boolean).forEach((file) => changed.add(file));
}

const mergeBase = process.env.GITHUB_BASE_REF
  ? (() => {
      try { return git(['merge-base', 'HEAD', `origin/${process.env.GITHUB_BASE_REF}`]); }
      catch (_) { return ''; }
    })()
  : '';
if (mergeBase) {
  const output = git(['diff', '--name-only', `${mergeBase}...HEAD`]);
  if (output) output.split(/\r?\n/).filter(Boolean).forEach((file) => changed.add(file));
}

const trackedDist = git(['ls-files', 'dist']);
if (trackedDist) trackedDist.split(/\r?\n/).filter(Boolean).forEach((file) => changed.add(file));

const forbidden = [...changed].filter((file) => file === 'dist' || file.startsWith('dist/'));

if (forbidden.length) {
  console.error('A pasta dist/ é artefato de build e não deve ser commitada. Rode npm run build:prod apenas antes do deploy.');
  console.error('\nArquivos dist/ detectados no PR/índice Git:');
  for (const file of forbidden.sort()) console.error(`- ${file}`);
  process.exit(1);
}

console.log('check:no-dist aprovado: nenhum artefato dist/ foi commitado.');

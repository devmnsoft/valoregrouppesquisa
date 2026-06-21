#!/usr/bin/env node
const { execFileSync } = require('child_process');
const forbiddenTracked = execFileSync('git', ['ls-files', 'dist'], { encoding: 'utf8' }).trim();
if (forbiddenTracked) {
  console.error('dist/ não deve ser versionado:\n' + forbiddenTracked);
  process.exit(1);
}
console.log('security-check aprovado: dist/ não está versionado.');

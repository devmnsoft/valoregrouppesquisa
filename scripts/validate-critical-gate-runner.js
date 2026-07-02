#!/usr/bin/env node

const { readFileSync } = require('fs');
const { join } = require('path');

const targetPath = join(__dirname, 'validate-regression-critical-gate.js');
const source = readFileSync(targetPath, 'utf8');

const failures = [];

function assertContains(description, snippet) {
  if (!source.includes(snippet)) {
    failures.push(`Ausente: ${description}`);
  }
}

assertContains('função runNpmScript', 'function runNpmScript(scriptName)');
assertContains('uso de process.env.npm_execpath', 'process.env.npm_execpath');
assertContains('uso de process.execPath para executar npm_execpath', 'process.execPath');
assertContains(
  'fallback Windows com shell habilitado apenas no Windows',
  "shell: process.platform === 'win32'"
);

const forbiddenDirectWindowsSpawn = /spawnSync\(\s*npmCommand\s*,\s*\[\s*['"]run['"]\s*,\s*scriptName\s*\]\s*,\s*\{[\s\S]*?shell\s*:\s*false[\s\S]*?\}\s*\)/m;
if (forbiddenDirectWindowsSpawn.test(source)) {
  failures.push("Uso proibido: spawnSync(npmCommand, ['run', scriptName], { shell: false })");
}

const runnerInvocation = /const\s+result\s*=\s*runNpmScript\(scriptName\)\s*;/;
if (!runnerInvocation.test(source)) {
  failures.push('O loop do check:critical deve chamar runNpmScript(scriptName).');
}

if (failures.length > 0) {
  console.error('validate-critical-gate-runner: FAIL');
  for (const failure of failures) {
    console.error(`- ${failure}`);
  }
  process.exit(1);
}

console.log('validate-critical-gate-runner: PASS');
console.log('Ambiente recomendado: Node.js 22 LTS para compatibilidade com Firebase Functions.');

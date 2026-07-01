#!/usr/bin/env node

const { spawnSync } = require('child_process');
const { readFileSync } = require('fs');
const { join } = require('path');

const criticalScripts = [
  'app:syntax',
  'actions:handlers',
  'public:boot-no-reference',
  'home:featured-consistency',
  'data:soft-delete-filtering',
  'routes:public-params',
  'public:submit-firebase-only',
  'firestore:no-undefined',
  'audit:no-save-loop',
  'lgpd:text',
  'public:survey-url-contract'
];

const packageJsonPath = join(__dirname, '..', 'package.json');
const packageJson = JSON.parse(readFileSync(packageJsonPath, 'utf8'));
const scripts = packageJson.scripts || {};

for (const scriptName of criticalScripts) {
  if (!Object.prototype.hasOwnProperty.call(scripts, scriptName)) {
    console.error(`Script crítico ausente: ${scriptName}`);
    process.exit(1);
  }
}

const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';

for (const scriptName of criticalScripts) {
  console.log(`\ncheck:critical > npm run ${scriptName}`);
  const result = spawnSync(npmCommand, ['run', scriptName], {
    cwd: join(__dirname, '..'),
    stdio: 'inherit',
    shell: false
  });

  if (result.error) {
    console.error(`Falha ao executar validador crítico: ${scriptName}`);
    console.error(result.error.message);
    process.exit(1);
  }

  if (result.status !== 0) {
    console.error(`Validador crítico falhou: ${scriptName}`);
    process.exit(result.status || 1);
  }
}

console.log('\ncheck:critical: PASS');

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

function runNpmScript(scriptName) {
  const root = join(__dirname, '..');
  const npmExecPath = process.env.npm_execpath;

  if (npmExecPath) {
    return spawnSync(process.execPath, [npmExecPath, 'run', scriptName], {
      cwd: root,
      stdio: 'inherit',
      shell: false,
      env: process.env
    });
  }

  const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm';

  return spawnSync(npmCommand, ['run', scriptName], {
    cwd: root,
    stdio: 'inherit',
    shell: process.platform === 'win32',
    env: process.env
  });
}

for (const scriptName of criticalScripts) {
  console.log(`\ncheck:critical > npm run ${scriptName}`);
  const result = runNpmScript(scriptName);

  if (result.error) {
    console.error(`Falha ao executar validador crítico: ${scriptName}`);
    console.error(`Erro: ${result.error.code || ''} ${result.error.message}`);
    console.error(`Node: ${process.version}`);
    console.error(`Platform: ${process.platform}`);
    console.error(`npm_execpath: ${process.env.npm_execpath || '(não informado)'}`);
    console.error(`Tente rodar manualmente: npm run ${scriptName}`);
    process.exit(1);
  }

  if (result.status !== 0) {
    console.error(`Validador crítico falhou: ${scriptName}`);
    process.exit(result.status || 1);
  }
}

console.log('\ncheck:critical: PASS');

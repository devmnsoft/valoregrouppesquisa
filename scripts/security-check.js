#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

const root = path.resolve(__dirname, '..');
const failures = [];
const isCi = process.env.CI === 'true' || process.env.GITHUB_ACTIONS === 'true';

function commandExists(command, args = ['--version']) {
  try {
    execFileSync(command, args, {
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'ignore'],
    });
    return true;
  } catch (_) {
    return false;
  }
}

function git(args) {
  return execFileSync('git', args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
}

function walkFiles(dir, list = []) {
  const ignoredDirs = new Set([
    '.git',
    'node_modules',
    'dist',
    'publish',
    'backups',
    'exports',
    'test-results',
    'playwright-report',
  ]);

  if (!fs.existsSync(dir)) return list;

  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (ignoredDirs.has(entry.name)) continue;

    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      walkFiles(full, list);
      continue;
    }
    if (entry.isFile()) list.push(path.relative(root, full).replace(/\\/g, '/'));
  }

  return list;
}

const sensitivePathPatterns = [
  /(^|\/)\.env(\.|$)/i,
  /(^|\/)serviceAccount.*\.json$/i,
  /(^|\/)secrets?\.json$/i,
  /(^|\/)firebase-debug\.log$/i,
  /(^|\/)data\/email_config\.json$/i,
  /(^|\/)data\/outbox\/(?!\.gitkeep$)/i,
  /(^|\/)dist\//i,
];
const sensitiveContentPatterns = [
  { name: 'BotToken', pattern: /\b\d{8,10}:[A-Za-z0-9_-]{30,}\b/ },
  { name: 'TELEGRAM_BOT_TOKEN', pattern: /TELEGRAM_BOT_TOKEN\s*[:=]\s*['\"]?[A-Za-z0-9:_-]{20,}/i },
  { name: 'SMTP_PASSWORD', pattern: /SMTP_PASSWORD\s*[:=]\s*['\"][^'\"\s$<]{8,}/i },
  { name: 'serviceAccount', pattern: /serviceAccount\s*[:=]\s*['\"]?\{?/i },
  { name: 'private_key', pattern: /-----BEGIN PRIVATE KEY-----|"private_key"\s*:\s*"-----BEGIN/i },
];

function checkFiles(files) {
  const allowedBinary = /\.(png|jpe?g|gif|webp|ico|pdf|woff2?)$/i;

  for (const file of files) {
    const normalized = file.replace(/\\/g, '/');
    if (sensitivePathPatterns.some((pattern) => pattern.test(normalized))) failures.push(`Arquivo sensível versionado/local: ${normalized}`);
    if (allowedBinary.test(normalized) || normalized === 'scripts/security-check.js' || /\.md$/i.test(normalized)) continue;
    const full = path.join(root, normalized);
    if (!fs.existsSync(full) || fs.statSync(full).isDirectory()) continue;
    const text = fs.readFileSync(full, 'utf8');
    for (const { name, pattern } of sensitiveContentPatterns) {
      if (pattern.test(text)) failures.push(`Token/segredo suspeito (${name}) em: ${normalized}`);
    }
  }
}

const gitAvailable = commandExists('git');
let fallbackMode = false;

if (!gitAvailable) {
  console.warn('Aviso: Git não foi encontrado no PATH.');
  console.warn('No Windows, instale o Git ou adicione C:\\Program Files\\Git\\cmd ao PATH.');
  console.warn('Executando verificação local limitada sem git ls-files.');

  if (fs.existsSync(path.join(root, 'dist'))) {
    console.warn('Aviso: dist/ existe localmente. Isso é normal após build, mas a pasta não deve ser commitada.');
    console.warn('Instale o Git para validar se dist/ está versionado.');
  }

  if (isCi) {
    failures.push('Git não encontrado no ambiente CI. O security-check exige Git no CI.');
  } else {
    fallbackMode = true;
    checkFiles(walkFiles(root));
  }
} else {
  const forbiddenTracked = git(['ls-files', 'dist']);
  if (forbiddenTracked) failures.push(`dist/ não deve ser versionado:\n${forbiddenTracked}`);

  const trackedFiles = git(['ls-files']).split(/\r?\n/).filter(Boolean);
  checkFiles(trackedFiles);
}

const firebaseJson = path.join(root, 'firebase.json');
if (fs.existsSync(firebaseJson)) {
  const text = fs.readFileSync(firebaseJson, 'utf8');
  const csp = [...text.matchAll(/"key"\s*:\s*"Content-Security-Policy"[\s\S]*?"value"\s*:\s*"([^"]+)"/g)].map((m) => m[1]).join('\n');
  if (/script-src[^;]*(^|\s)\*(\s|;|$)/i.test(csp)) failures.push('CSP insegura: script-src * não é permitido.');
  if (/connect-src[^;]*(^|\s)\*(\s|;|$)/i.test(csp)) failures.push('CSP insegura: connect-src * não é permitido.');
  if (/unsafe-eval/i.test(csp)) failures.push('CSP insegura: unsafe-eval não é permitido.');
}

if (failures.length) {
  console.error('Falha no security-check:\n' + failures.map((f) => `- ${f}`).join('\n'));
  process.exit(1);
}

if (fallbackMode) {
  console.log('security-check aprovado em modo fallback local.');
  console.log('Atenção: Git não foi encontrado; não foi possível validar arquivos versionados.');
  console.log('Instale o Git para validação completa.');
} else {
  console.log('security-check aprovado: sem dist versionado, segredos óbvios ou CSP insegura.');
}

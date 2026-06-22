#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

const root = path.resolve(__dirname, '..');
const failures = [];
const isCi = process.env.CI === 'true' || process.env.GITHUB_ACTIONS === 'true';
const ignoredFallbackDirs = new Set(['node_modules', '.git', 'dist', 'test-results', 'playwright-report']);

function hasGit() {
  try {
    execFileSync('git', ['--version'], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
    return true;
  } catch (_) {
    return false;
  }
}

function git(args) {
  try {
    return execFileSync('git', args, { encoding: 'utf8' }).trim();
  } catch (err) {
    if (err.code === 'ENOENT') {
      throw new Error(
        'Git não encontrado no PATH. Instale o Git ou adicione C:\\Program Files\\Git\\cmd ao PATH do Windows.'
      );
    }
    throw err;
  }
}

function walkFiles(dir, list = []) {
  if (!fs.existsSync(dir)) return list;

  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (ignoredFallbackDirs.has(entry.name)) continue;

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

const gitAvailable = hasGit();
let fallbackMode = false;

if (!gitAvailable) {
  console.warn('Aviso: Git não foi encontrado. A verificação será feita nos arquivos locais, mas não será possível validar arquivos versionados com git ls-files.');
  console.warn('Para corrigir no Windows, instale o Git ou adicione C:\\Program Files\\Git\\cmd ao PATH. Teste com: git --version');

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
  console.log('security-check aprovado em modo fallback local. Instale o Git para validar arquivos versionados.');
} else {
  console.log('security-check aprovado.');
}

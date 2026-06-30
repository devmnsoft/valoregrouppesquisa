#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const dist = path.join(root, 'dist');
const failures = [];

function walk(dir) {
  if (!fs.existsSync(dir)) return [];
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    return entry.isDirectory() ? walk(full) : [full];
  });
}

if (!fs.existsSync(dist)) {
  console.log('dist/ ainda não existe; execute npm run build:prod antes do deploy.');
  process.exit(0);
}

const forbiddenNames = [/\.map$/i, /(^|\/)\.env(\.|$)/i, /serviceAccount/i, /secrets?/i, /data\/outbox/i, /email_config\.json$/i];
const forbiddenContent = [/\b\d{8,10}:[A-Za-z0-9_-]{30,}\b/, /TELEGRAM_BOT_TOKEN/i];
const forbiddenSources = new Set(['app.js','style.css','firebase-repository.js','local-repository.js','repository.js']);

for (const file of walk(dist)) {
  const rel = path.relative(root, file).replace(/\\/g, '/');
  if (forbiddenNames.some((pattern) => pattern.test(rel))) failures.push(`Arquivo proibido no build: ${rel}`);
  if (forbiddenSources.has(path.basename(file))) failures.push(`Fonte original não hashado no build: ${rel}`);
  const buffer = fs.readFileSync(file);
  if (buffer.includes(Buffer.from('sourceMappingURL'))) failures.push(`Referência a source map em: ${rel}`);
  if (/\.(html|js|css|json|txt)$/i.test(file)) {
    const text = buffer.toString('utf8');
    for (const pattern of forbiddenContent) if (pattern.test(text)) failures.push(`Conteúdo sensível potencial em: ${rel}`);
  }
}

if (failures.length) {
  console.error('Falha no postbuild-security-check:\n' + failures.map((f) => `- ${f}`).join('\n'));
  process.exit(1);
}
console.log('postbuild-security-check aprovado.');

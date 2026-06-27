const fs = require('fs');
const path = require('path');

const roots = ['backend', 'scripts', 'migration'];
const bad = [
  'ILogger<',
  'catch (Exception ex)',
  'logger.LogError',
  'LogError(ex',
  'throw;',
  'validate:',
  'PASS',
  'Sprint 24 operational logging contract',
  'Sprint 25 operational logging contract'
];
const exts = new Set(['.cs', '.js', '.ts', '.json']);

function files(dir) {
  if (!fs.existsSync(dir)) return [];
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const current = path.join(dir, entry.name);
    if (entry.isDirectory() && !['node_modules', 'bin', 'obj', '.git'].includes(entry.name)) return files(current);
    return entry.isFile() && exts.has(path.extname(current)) ? [current] : [];
  });
}

function extractComments(source) {
  const comments = [];
  let i = 0;
  let state = 'code';
  let quote = '';
  let current = '';

  while (i < source.length) {
    const ch = source[i];
    const next = source[i + 1];

    if (state === 'code') {
      if (ch === '/' && next === '/' && source[i - 1] !== '\\') {
        state = 'line';
        current = '//';
        i += 2;
        continue;
      }
      if (ch === '/' && next === '*' && source[i - 1] !== '\\') {
        state = 'block';
        current = '/*';
        i += 2;
        continue;
      }
      if (ch === '"' || ch === "'" || ch === '`') {
        quote = ch;
        state = 'string';
      }
      i += 1;
      continue;
    }

    if (state === 'string') {
      if (ch === '\\') {
        i += 2;
        continue;
      }
      if (ch === quote) state = 'code';
      i += 1;
      continue;
    }

    if (state === 'line') {
      if (ch === '\n') {
        comments.push(current);
        current = '';
        state = 'code';
      } else {
        current += ch;
      }
      i += 1;
      continue;
    }

    if (state === 'block') {
      current += ch;
      if (ch === '*' && next === '/') {
        current += '/';
        comments.push(current);
        current = '';
        state = 'code';
        i += 2;
      } else {
        i += 1;
      }
    }
  }

  if (current) comments.push(current);
  return comments;
}

const hits = [];
for (const file of roots.flatMap(files)) {
  const source = fs.readFileSync(file, 'utf8');
  for (const comment of extractComments(source)) {
    if (comment.length > 300) continue;
    for (const forbidden of bad) {
      if (comment.includes(forbidden)) hits.push(`${file}: comentário falso contém ${forbidden}`);
    }
  }
}

if (hits.length) {
  console.error(hits.join('\n'));
  process.exit(1);
}

console.log('validate-no-validator-fake-comments: PASS');

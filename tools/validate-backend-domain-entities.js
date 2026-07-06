#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

const repo = path.resolve(__dirname, '..');
const entitiesRoot = path.join(repo, 'backend', 'Valora.Domain', 'Entities');
const backendRoot = path.join(repo, 'backend');
const generatedPattern = /([\\/](bin|obj|Generated)[\\/]|\.g\.cs$|\.Designer\.cs$)/i;

function walk(dir, predicate = () => true) {
  if (!fs.existsSync(dir)) return [];
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full, predicate));
    else if (predicate(full)) out.push(full);
  }
  return out;
}

function rel(file) { return path.relative(repo, file).replace(/\\/g, '/'); }

function declarations(file) {
  const text = fs.readFileSync(file, 'utf8');
  const nsMatch = text.match(/namespace\s+([A-Za-z0-9_.]+)/);
  const namespace = nsMatch ? nsMatch[1] : '<global>';
  const decls = [];
  const re = /\b(?:public|internal|private|protected)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*\b(class|record|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)/g;
  let m;
  while ((m = re.exec(text))) decls.push({ namespace, kind: m[1], name: m[2], file: rel(file) });
  return decls;
}

let hasError = false;
const decls = walk(entitiesRoot, f => f.endsWith('.cs') && !generatedPattern.test(f)).flatMap(declarations);
const groups = new Map();
for (const d of decls) {
  const key = `${d.namespace}::${d.name}`;
  if (!groups.has(key)) groups.set(key, []);
  groups.get(key).push(d);
}

for (const [key, items] of [...groups.entries()].sort()) {
  const distinctFiles = [...new Set(items.map(i => i.file))];
  if (items.length > 1) {
    hasError = true;
    const [namespace, name] = key.split('::');
    console.error(`CS0101 provável: ${namespace}.${name} aparece ${items.length} vezes:`);
    for (const item of items) console.error(`  - ${item.kind}: ${item.file}`);
  }
}

for (const aggregator of ['MigrationDomainEntities.cs', 'OperationalEntities.cs']) {
  const file = path.join(entitiesRoot, aggregator);
  if (!fs.existsSync(file)) continue;
  for (const d of declarations(file)) {
    const expected = `backend/Valora.Domain/Entities/${d.name}.cs`;
    if (d.file !== expected && fs.existsSync(path.join(repo, expected))) {
      hasError = true;
      console.error(`${aggregator} declara ${d.name}, que também existe em arquivo próprio (${expected}).`);
    }
  }
}

const longLines = [];
const singleLineTypes = [];
for (const file of walk(backendRoot, f => f.endsWith('.cs') && !generatedPattern.test(f))) {
  const text = fs.readFileSync(file, 'utf8');
  text.split(/\r?\n/).forEach((line, idx) => {
    if (line.length > 220) longLines.push(`${rel(file)}:${idx + 1} (${line.length})`);
    if (/\b(class|record)\s+[A-Za-z_][A-Za-z0-9_]*[^\n]*\{[^\n]*\}[^\n]*$/.test(line) && line.length > 100) {
      singleLineTypes.push(`${rel(file)}:${idx + 1}`);
    }
  });
}

if (longLines.length) {
  console.warn(`Aviso: ${longLines.length} linhas C# acima de 220 caracteres.`);
  for (const item of longLines.slice(0, 30)) console.warn(`  - ${item}`);
}
if (singleLineTypes.length) {
  console.warn(`Aviso: ${singleLineTypes.length} classes/records possivelmente em uma única linha.`);
  for (const item of singleLineTypes.slice(0, 30)) console.warn(`  - ${item}`);
}

if (hasError) process.exit(1);
console.log(`OK: ${decls.length} declarações de domínio sem duplicidade de nome/namespace.`);

#!/usr/bin/env node
const fs = require('node:fs');
const path = require('node:path');

const backend = path.resolve(__dirname, '..', '..');
const files = [];
function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (entry.name === 'bin' || entry.name === 'obj' || entry.name === 'TestResults') continue;
    const full = path.join(directory, entry.name);
    entry.isDirectory() ? walk(full) : files.push(full);
  }
}
walk(backend);

const failures = [];
const tests = files.filter(file => file.endsWith('Tests.cs'));
for (const file of tests) {
  const source = fs.readFileSync(file, 'utf8');
  if (/\bclass\s+\w*Tests\b/.test(source) && !/\[Trait\("Category",\s*"(?:Unit|Architecture|StaticContract|DatabaseContract|Integration|BffIntegration|EndToEnd|LegacyCompatibility)"\)\]/.test(source)) {
    failures.push(`test without an official category: ${path.relative(backend, file)}`);
  }
  if (/(LocateRepositoryRoot|FindRepositoryRoot|AppContext\.BaseDirectory\s*[,+])/.test(source)) {
    failures.push(`duplicated repository-root lookup: ${path.relative(backend, file)}`);
  }
}

const enterpriseControllers = /(?:BusinessGroups|LegalEntities|Units|Departments|Dashboard)Controller\.cs$/;
for (const file of files.filter(file => enterpriseControllers.test(file))) {
  const source = fs.readFileSync(file, 'utf8');
  if (/JsonElement|Dictionary<string,\s*object\?>/.test(source)) failures.push(`untyped controller contract: ${path.relative(backend, file)}`);
}

const joined = files.map(file => path.relative(backend, file).replaceAll('\\', '/')).join('\n');
for (const forbidden of ['backend/backend', 'Valora.Tests/Valora.Api', 'Valora.Tests/Valora.Infrastructure']) {
  if (joined.includes(forbidden)) failures.push(`forbidden path: ${forbidden}`);
}

if (failures.length) {
  console.error(failures.join('\n'));
  process.exit(1);
}
console.log(`Phase 2H.2 static gate passed (${tests.length} test files inspected).`);

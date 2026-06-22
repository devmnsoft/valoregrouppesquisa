#!/usr/bin/env node
'use strict';
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const targets = [
  { p: path.join(root, 'data', 'outbox'), onlyEml: true },
  { p: path.join(root, 'test-results') },
  { p: path.join(root, 'playwright-report') }
];
let removed = 0;
for (const target of targets) {
  if (!fs.existsSync(target.p)) continue;
  if (target.onlyEml) {
    for (const file of fs.readdirSync(target.p)) {
      if (file.toLowerCase().endsWith('.eml')) {
        fs.rmSync(path.join(target.p, file), { force: true });
        removed++;
      }
    }
  } else {
    fs.rmSync(target.p, { recursive: true, force: true });
    removed++;
  }
}
console.log(`Limpeza segura concluída. Itens removidos: ${removed}.`);

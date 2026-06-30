const fs = require('fs');
function fail(message) { console.error(message); process.exit(1); }
function readJson(file) { return JSON.parse(fs.readFileSync(file, 'utf8')); }
const firebase = readJson('firebase.json');
const pkg = readJson('functions/package.json');
const lint = String(pkg.scripts?.lint || '');
if (firebase.functions?.source !== 'functions') fail('firebase.json functions.source deve ser functions');
if (firebase.functions?.runtime !== 'nodejs22') fail('firebase.json deve usar functions.runtime nodejs22');
if (String(pkg.engines?.node) !== '22') fail('functions/package.json engines.node deve ser 22');
for (const dep of ['firebase-functions', 'firebase-admin', 'nodemailer']) {
  if (!pkg.dependencies?.[dep]) fail(`functions/package.json sem dependência ${dep}`);
}
if (lint !== 'node scripts/check-functions-syntax.js') fail('functions/package.json lint deve usar node scripts/check-functions-syntax.js');
if (lint.includes('utils/*.js')) fail('functions/package.json lint não pode usar utils/*.js');
if (!fs.existsSync('functions/scripts/check-functions-syntax.js')) fail('functions/scripts/check-functions-syntax.js ausente');
console.log('functions:node22-readiness OK');

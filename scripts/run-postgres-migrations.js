#!/usr/bin/env node
const { spawnSync } = require('child_process');
const files = require('fs').readdirSync('database/postgresql').filter(f=>f.endsWith('.sql')).sort();
for (const file of files) {
  console.log(`Applying ${file}`);
  const r = spawnSync('docker', ['exec','-i','valora-postgres','psql','-U','valora','-d','valoradb'], { input: require('fs').readFileSync(`database/postgresql/${file}`), stdio:['pipe','inherit','inherit'] });
  if (r.status) process.exit(r.status);
}

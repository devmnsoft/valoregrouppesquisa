#!/usr/bin/env node
const r=require('child_process').spawnSync(process.execPath,['scripts/run-postgres-migrations.js'],{stdio:'inherit'});
process.exit(r.status||0);

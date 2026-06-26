#!/usr/bin/env node
const fs=require('fs');
const firebasePath=process.argv[2]||'migration/firestore-export.json';const postgresPath=process.argv[3]||'migration/postgres-import.json';
const critical=['organizations','users','surveys','responses','certificates','communications'];
function load(p){return JSON.parse(fs.readFileSync(p,'utf8'))}const f=load(firebasePath),p=load(postgresPath);let fail=false;
for(const key of critical){const fc=key==='organizations'?((f.collections.organizations||[]).length+(f.collections.companies||[]).length):(f.collections[key]||[]).length;const pc=(p[key]||[]).length;const ok=fc===pc;console.log(`${ok?'OK':'CRITICAL'} ${key}: firebase=${fc} postgres=${pc}`);if(!ok)fail=true;}
process.exit(fail?2:0);

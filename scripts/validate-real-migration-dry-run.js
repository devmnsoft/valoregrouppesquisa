#!/usr/bin/env node
const {spawnSync}=require('child_process');const fs=require('fs');
function run(cmd,args){const r=spawnSync(cmd,args,{encoding:'utf8'});if(r.status!==0)throw new Error(`${cmd} ${args.join(' ')} falhou: ${r.stderr||r.stdout}`);return r.stdout;}
run('node',['migration/export-firestore.js','--dry-run']);
run('node',['migration/transform-firestore-to-postgres.js','--dry-run']);
run('node',['migration/import-postgres.js','--dry-run']);
run('node',['migration/compare-firebase-postgres.js','--dry-run']);
if(!fs.existsSync('reports/migration-comparison.json'))throw new Error('reports/migration-comparison.json não gerado');
const leaked=/"password"\s*:\s*"(?!\*\*\*|\[redacted\]|)[^"]{3,}"|senha\s*[:=]/i.test(JSON.stringify(fs.existsSync('migration/out/users.json')?JSON.parse(fs.readFileSync('migration/out/users.json','utf8')):[]));
if(leaked)throw new Error('possível senha em texto puro encontrada em migration/out/users.json');
console.log('validate-real-migration-dry-run: PASS');

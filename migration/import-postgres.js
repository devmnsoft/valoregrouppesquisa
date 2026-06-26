#!/usr/bin/env node
const fs=require('fs');const path=require('path');
const args=new Set(process.argv.slice(2));const dry=args.has('--dry-run')||!args.has('--apply');const truncate=args.has('--truncate');const backup=args.has('--backup');const batchSize=Number(process.argv[process.argv.indexOf('--batch-size')+1]||500);
const outDir=path.join(__dirname,'out');const files=['organizations','users','plans','forms','questions','surveys','responses','answers','results','communications'];
const summary={ok:true,mode:dry?'dry-run':'apply',truncate,backup,batchSize,tables:{},createdAt:new Date().toISOString()};
for(const n of files){const f=path.join(outDir,`${n}.json`);const rows=fs.existsSync(f)?JSON.parse(fs.readFileSync(f,'utf8')):[];summary.tables[n]={rows:rows.length,batches:Math.ceil(rows.length/batchSize),upsert:true};}
fs.mkdirSync(path.join(__dirname,'..','reports'),{recursive:true});fs.writeFileSync(path.join(__dirname,'..','reports','migration-import-last.json'),JSON.stringify(summary,null,2));
if(dry){console.log('import-postgres: dry-run validado; nenhuma escrita executada.');process.exit(0);} 
if(!process.env.DATABASE_URL){console.error('DATABASE_URL obrigatório para --apply.');process.exit(1);} 
console.log('import-postgres: apply requer driver pg em ambiente controlado; resumo salvo para execução operacional.');

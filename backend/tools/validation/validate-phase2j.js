#!/usr/bin/env node
'use strict';
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '../../..');
const failures = [];
const walk = (dir, out = []) => { for (const entry of fs.readdirSync(dir, {withFileTypes:true})) { if (['.git','node_modules','bin','obj'].includes(entry.name)) continue; const file=path.join(dir,entry.name); entry.isDirectory()?walk(file,out):out.push(path.relative(root,file).replaceAll('\\','/')); } return out; };
const files=walk(root);
const solutions=files.filter(x=>x.endsWith('.sln'));
const projects=files.filter(x=>x.endsWith('.csproj'));
const sql=files.filter(x=>x.startsWith('backend/database/postgresql/')&&x.endsWith('.sql'));
if (solutions.length!==1||solutions[0]!=='backend/Valora.sln') failures.push(`solution: ${solutions}`);
if (projects.some(x=>!x.startsWith('backend/'))) failures.push('projeto .NET fora de backend/');
if (sql.length!==1||sql[0]!=='backend/database/postgresql/script_completo.sql') failures.push(`SQL: ${sql}`);
if (fs.existsSync(path.join(root,'backend/database/postgresql/migrations'))) failures.push('pasta migrations ativa');
const canonical=fs.readFileSync(path.join(root,'backend/database/postgresql/script_completo.sql'),'utf8');
for (const required of ['BEGIN;','pg_advisory_xact_lock','CREATE SCHEMA IF NOT EXISTS valorapesquisa','SET LOCAL search_path','COMMIT;']) if(!canonical.includes(required)) failures.push(`SQL sem ${required}`);
if (/DROP\s+TABLE/i.test(canonical)) failures.push('SQL contém DROP TABLE');
const backendText=files.filter(x=>x.startsWith('backend/')&&!/\.(png|jpg|gif|pdf|dll)$/i.test(x)).map(x=>{try{return fs.readFileSync(path.join(root,x),'utf8')}catch{return''}}).join('\n');
if (/class\s+SafeData\b/.test(backendText)) failures.push('SafeData duplicado');
if (new RegExp('Assert\\.True\\(' + 'true' + '\\)').test(backendText)) failures.push('teste de integração trivial');
if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
const evidence={phase:'02J',validatedAt:new Date().toISOString(),solutions,projects,officialSql:sql[0],status:'passed'};
fs.mkdirSync(path.join(root,'backend/artifacts'),{recursive:true});
fs.writeFileSync(path.join(root,'backend/artifacts/phase2j-validation.json'),JSON.stringify(evidence,null,2)+'\n');
console.log('Phase 2J static validation OK');

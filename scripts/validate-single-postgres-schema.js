#!/usr/bin/env node
const fs=require('fs'),path=require('path');
function files(d){return fs.existsSync(d)?fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>{const p=path.join(d,e.name);return e.isDirectory()?files(p):[p]}):[]}
const sql=files('database/postgresql').filter(f=>f.endsWith('.sql'));
const bad=/\b(valora|billing|communication|audit|migration)\.|CREATE\s+SCHEMA\s+IF\s+NOT\s+EXISTS\s+(valora|billing|communication|audit|migration)\b/i;
let errors=[];
for(const f of sql){const s=fs.readFileSync(f,'utf8'); if(bad.test(s)) errors.push(`${f}: schema legado`);}
if(!sql.some(f=>fs.readFileSync(f,'utf8').includes('valorapesquisa.'))) errors.push('nenhum SQL usa valorapesquisa.');
if(errors.length){console.error(errors.join('\n'));process.exit(1)}
console.log('validate-single-postgres-schema: PASS');

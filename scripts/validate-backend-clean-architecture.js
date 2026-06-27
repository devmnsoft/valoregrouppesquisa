#!/usr/bin/env node
const fs=require('fs'),path=require('path');const {stripComments}=require('./lib/strip-comments');
function files(d){return fs.existsSync(d)?fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>{const p=path.join(d,e.name);return e.isDirectory()?files(p):[p]}):[]}
let errors=[]; const cs=files('backend').filter(f=>f.endsWith('.cs'));
for(const f of cs){const s=stripComments(fs.readFileSync(f,'utf8')); const pubs=(s.match(/public\s+(?:sealed\s+|static\s+|readonly\s+|partial\s+)*(?:class|record|interface|enum)\s+/g)||[]).length; if(pubs>1 && !f.endsWith('Program.cs') && !f.includes('Valora.Tests')) errors.push(`${f}: múltiplos tipos públicos`);}
for(const f of files('backend/Valora.Api/Controllers').filter(f=>f.endsWith('.cs'))){const s=stripComments(fs.readFileSync(f,'utf8')); if(/(INSERT|UPDATE|DELETE) /i.test(s) || /SELECT (?!1;)/i.test(s)) errors.push(`${f}: SQL direto em controller`);}
for(const f of files('backend/Valora.Infrastructure/Repositories').filter(f=>f.endsWith('.cs'))){const s=stripComments(fs.readFileSync(f,'utf8')); if(!s.includes('Dapper')) errors.push(`${f}: repository sem Dapper`);}
if(errors.length){console.error(errors.join('\n'));process.exit(1)} console.log('validate-backend-clean-architecture: PASS');

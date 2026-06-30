#!/usr/bin/env node
const fs=require('fs'), path=require('path');
function walk(d){return fs.existsSync(d)?fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>{const p=path.join(d,e.name);return e.isDirectory()?walk(p):[p];}):[];}
const files=walk('backend/Valora.Web').filter(f=>/\.(cshtml|js|css|cs)$/.test(f));
const all=files.map(f=>fs.readFileSync(f,'utf8')).join('\n');
for(const t of ['publicToken','mobile','Bootstrap','jQuery']){if(!new RegExp(t,'i').test(all)){console.error('Paridade Valora.Web ausente: '+t);process.exit(1);}}
console.log('Paridade ASP.NET Web validada.');

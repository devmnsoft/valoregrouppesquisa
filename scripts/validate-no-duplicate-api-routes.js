#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const ROOT = path.join(process.cwd(), 'backend', 'Valora.Api', 'Controllers');
const VERBS = ['HttpGet','HttpPost','HttpPut','HttpPatch','HttpDelete'];
function walk(dir){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(d=>{const p=path.join(dir,d.name);return d.isDirectory()?walk(p):(d.name.endsWith('.cs')?[p]:[]);});}
function normalize(route){return (route||'').trim().replace(/^~/,'').replace(/^\//,'').replace(/:guid|:int|:long|:decimal|:bool|:datetime/g,'').replace(/\/+$/,'').toLowerCase() || '/';}
const seen=new Map(), dup=[];
for(const file of walk(ROOT)){
 const text=fs.readFileSync(file,'utf8');
 const re=/\[(ApiExplorerSettings\(IgnoreApi\s*=\s*true\)\][\s\S]{0,160}?\[)?(HttpGet|HttpPost|HttpPut|HttpPatch|HttpDelete)\("([^"]*)"\)\]/g;
 let m; while((m=re.exec(text))){ if(m[1]) continue; const key=m[2].replace('Http','').toUpperCase()+' '+normalize(m[3]); const val=path.relative(process.cwd(),file)+':'+(text.slice(0,m.index).split('\n').length); if(seen.has(key)) dup.push(`${key}\n  - ${seen.get(key)}\n  - ${val}`); else seen.set(key,val); }
}
if(dup.length){console.error('Rotas duplicadas encontradas:\n'+dup.join('\n')); process.exit(1);} console.log(`validate-api-no-route-conflicts: PASS (${seen.size} rotas)`);

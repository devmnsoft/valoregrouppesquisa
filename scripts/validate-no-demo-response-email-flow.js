#!/usr/bin/env node
const fs=require('fs'),path=require('path');
const terms=[['resp','demo'].join('_'),['demo','response'].join('_'),['response','demo'].join('_'),['pending-provider','local.invalid'].join('@')];
const allowed=f=>/\.(test|spec)\./.test(f)||f.endsWith('.md');const skip=new Set(['node_modules','.git','dist','reports','backups']);const hits=[];
function walk(d){for(const e of fs.readdirSync(d,{withFileTypes:true})){const p=path.join(d,e.name);if(e.isDirectory()){if(!skip.has(e.name))walk(p);continue;}if(!/\.(js|cs|cshtml|json|bat|yml|yaml|html)$/.test(e.name)||allowed(p))continue;const t=fs.readFileSync(p,'utf8');for(const term of terms)if(t.includes(term))hits.push({file:p,term});}}
walk('.');fs.mkdirSync('reports',{recursive:true});fs.writeFileSync('reports/no-demo-response-report.json',JSON.stringify({status:hits.length?'FAIL':'PASS',hits},null,2));
if(hits.length){console.error(hits.map(h=>`${h.file}: contém ${h.term}`).join('\n'));process.exit(1)}console.log('validate-no-demo-response-email-flow: PASS');

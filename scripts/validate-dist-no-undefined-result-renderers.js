#!/usr/bin/env node
const fs=require('fs'),path=require('path');function fail(m){console.error('FAIL:',m);process.exit(1)}
function walk(dir,out=[]){if(!fs.existsSync(dir))return out;for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory())walk(p,out);else if(/^app(?:\.[\w-]+)?\.js$/.test(e.name)||e.name==='app.js')out.push(p);}return out;}
const files=[...walk('dist'),...walk('public')];
for(const f of files){const s=fs.readFileSync(f,'utf8');if(s.includes('renderPremiumPublicResult(')&&!/function\s+renderPremiumPublicResult\s*\(/.test(s))fail(`${f} chama renderPremiumPublicResult sem função`);if(s.includes('renderBasicPublicResult(')&&!/function\s+renderBasicPublicResult\s*\(/.test(s))fail(`${f} chama renderBasicPublicResult sem função`);}
if(!files.length) console.warn('WARN: nenhum app*.js em dist/public para validar');
console.log('dist no undefined result renderers: PASS');

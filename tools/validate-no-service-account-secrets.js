#!/usr/bin/env node
const fs=require('fs');const path=require('path');
const root=process.cwd();const findings=[];
const skip=new Set(['.git','node_modules','bin','obj','dist','build','coverage']);
function walk(dir){for(const ent of fs.readdirSync(dir,{withFileTypes:true})){if(skip.has(ent.name))continue;const p=path.join(dir,ent.name);if(ent.isDirectory())walk(p);else check(p);}}
function rel(p){return path.relative(root,p).replace(/\\/g,'/');}
function check(file){const r=rel(file);const ext=path.extname(file).toLowerCase();let text='';try{text=fs.readFileSync(file,'utf8');}catch{return;}
 if(ext==='.json'){
  try{const json=JSON.parse(text); if(json && json.type==='service_account') findings.push(`${r}: JSON service_account`);}catch{}
  if(/firebase-adminsdk/i.test(r)) findings.push(`${r}: nome de credencial Firebase Admin SDK`);
 }
 const protectedArea=/^(backend\/|database\/postgresql\/|scriptbd_completo\.sql$)/.test(r) || /\/wwwroot\//.test(r);
 const patterns=[[/-----BEGIN PRIVATE KEY-----/,'bloco de chave privada'],[/"private_key"\s*:/,'campo private_key'],[/"client_email"\s*:\s*"[^"]+@[^"]+\.iam\.gserviceaccount\.com"/,'e-mail de service account'],[/firebase-adminsdk-[\w-]+\.json/i,'arquivo de credencial Firebase Admin SDK']];
 for(const [re,label] of patterns){ if(re.test(text) && protectedArea) findings.push(`${r}: ${label}`); }
}
walk(root);
if(findings.length){console.error('Credenciais de service account detectadas:\n'+findings.join('\n'));process.exit(1);} 
console.log('OK: nenhuma credencial de service account versionada nas áreas oficiais.');

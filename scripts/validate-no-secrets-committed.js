#!/usr/bin/env node
'use strict';
const fs=require('fs');const path=require('path');
const root=path.resolve(__dirname,'..');const ignore=new Set(['node_modules','.git','reports','backups','dist']);
const allow=new Set(['scripts/validate-no-secrets-committed.js','SPRINT_74_LEGACY_CLIENT_USER_AND_DIST_FIX_AUDIT.md']);
const patterns=[
  [/SMTP_PASSWORD\s*[:=]\s*['\"](?!auto|secret|example|placeholder|process\.env|\$\{|<)[^'\"]{8,}['\"]/i,'SMTP_PASSWORD com valor real'],
  [/private_key\s*[:=]\s*['\"]-----BEGIN PRIVATE KEY-----/i,'private_key'],
  [/type\s*[:=]\s*['\"]service_account['\"]/i,'service account'],
  [/(?:publicToken|resultToken)\s*[:=]\s*['\"][A-Za-z0-9_-]{32,}['\"]/,'token completo'],
  [/tokenHash=[A-Fa-f0-9]{32,}/,'tokenHash exposto como URL'],
  [/collection\(['\"]users['\"]\)[\s\S]{0,500}password\s*[:=]\s*['\"][^'\"]{6,}['\"]/i,'senha de usuário em claro salva em Firestore'],
  [/adminPassword\s*[:=]\s*['\"][^'\"]{6,}['\"]/i,'senha admin em claro'],
  [/GOOGLE_APP_PASSWORD\s*[:=]\s*['\"][^'\"]{8,}['\"]/i,'app password Google potencial']
];
function walk(d,out=[]){for(const e of fs.readdirSync(d,{withFileTypes:true})){if(ignore.has(e.name))continue;const f=path.join(d,e.name);if(e.isDirectory())walk(f,out);else if(e.isFile())out.push(f);}return out;}
const hits=[];for(const file of walk(root)){const rel=path.relative(root,file).replace(/\\/g,'/');if(allow.has(rel))continue;if(!/\.(js|json|env|yml|yaml)$/i.test(rel))continue;const s=fs.readFileSync(file,'utf8');for(const [re,label] of patterns){if(re.test(s))hits.push(`${rel}: ${label}`);}}
if(hits.length)throw new Error(`Segredos ou credenciais potenciais encontrados:\n${hits.join('\n')}`);
console.log('validate-no-secrets-committed: PASS');

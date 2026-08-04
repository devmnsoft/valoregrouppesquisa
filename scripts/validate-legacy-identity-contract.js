#!/usr/bin/env node
'use strict';
const fs=require('fs');const path=require('path');
const root=path.resolve(__dirname,'..');
const forbidden=/Valora\s*Pulse(?:™)?|ValoraPulse|Valora\s*Pulso/i;
const ignoreDirs=new Set(['.git','node_modules','backups','publish']);
const scanRoots=['app.js','data-normalization.js','local-repository.js','firebase-repository.js','config.js','config','index.html','scripts','tests','functions','backend','templates','dist','exports','reports','server.py','package.json'];
const allow=[
 {file:'config.js',re:/LEGACY_STORE_KEYS|valoraPulseFinal800/,why:'chave técnica legada para migração'},
 {file:'config/config.production.js',re:/LEGACY_STORE_KEYS|valoraPulseFinal800/,why:'chave técnica legada para migração'},
 {file:'config/config.local.js',re:/STORE_KEY.*valoraPulseFinal800/,why:'compatibilidade local legada'},
 {file:'config/config.local-api.js',re:/STORE_KEY.*valoraPulseFinal800/,why:'compatibilidade local legada'},
 {file:'config/config.local-firebase.js',re:/STORE_KEY.*valoraPulseFinal800/,why:'compatibilidade local legada'},
 {file:'config/config.hybrid.js',re:/STORE_KEY.*valoraPulseFinal800/,why:'compatibilidade local legada'},
 {file:'config.local.js',re:/STORE_KEY.*valoraPulseFinal800/,why:'compatibilidade local legada'},
 {file:'local-repository.js',re:/valoraPulseFinal800|LEGACY_STORE_KEYS/,why:'migração do localStorage legado'},
 {file:'firebase-repository.js',re:/ValoraPulse|valoraPulse/,why:'limpeza técnica de chaves locais antigas'},
 {file:'scripts/firebase-seed-utils.js',re:/valoraPulseFinal800/,why:'metadado técnico de export legado'},
 {file:'scripts/migrate-valora-insight-visible-branding.js',re:/Valora\\s\*|ValoraPulse|Valora Pulso|Pulse/,why:'regex/teste da migração'},
 {file:'scripts/validate-legacy-identity-contract.js',re:/Valora|Pulse|Pulso|valoraPulseFinal800/,why:'validator precisa conter padrões proibidos'},
 {file:'scripts/healthcheck-prd.js',re:/Valora|Pulse|Pulso/,why:'health check precisa conter padrões proibidos'},
 {file:'scripts/validate-build-info-log.js',re:/Valora Pulse/,why:'validator comprova remoção de log antigo'},
 {file:'scripts/validate-public-auth-timeout.js',re:/Valora Pulse/,why:'validator comprova remoção de warning antigo'},
 {file:'scripts/validate-legacy-public-texts.js',re:/Valora Pulse/,why:'validator comprova ausência no app ativo'},
 {file:'scripts/validate-final-regression-lockdown.js',re:/Valora Pulse/,why:'validator comprova ausência no app ativo'},
 {file:'scripts/validate-legacy-public-admin-branding.js',re:/Valora Pulse/,why:'validator comprova ausência no app ativo'},
 {file:'scripts/validate-legacy-certificate-premium-layout.js',re:/Valora Pulse/,why:'validator comprova ausência em certificado'},
 {file:'scripts/validate-legacy-public-brand-insight.js',re:/Valora Pulse/,why:'validator comprova ausência em resultado público'},
 {file:'scripts/validate-legacy-report-structure.js',re:/Valora Pulse/,why:'validator comprova ausência em relatório'},
 {file:'scripts/validate-valora-web-ui-parity.js',re:/Valora Pulse/,why:'validator legado de paridade ASP.NET'},
 {file:'scripts/validate-certificate-rich-content.js',re:/Valora Pulse/,why:'validator legado de certificado'},
 {file:'tests',re:/Valora Pulse|ValoraPulse|Valora Pulso|valoraPulseFinal800/,why:'fixtures comprovam conversão/compatibilidade'},
 {file:'data-normalization.js',re:/Pulse|Pulso|ValoraPulse/,why:'normalizador central precisa reconhecer marca antiga'},
 {file:'dist/config.js',re:/LEGACY_STORE_KEYS|valoraPulseFinal800/,why:'chave técnica legada no build'},
 {file:'dist/assets',re:/LEGACY_VISIBLE_BRAND_PATTERN|Pulse|Pulso|ValoraPulse|valoraPulseFinal800/,why:'bundle contém somente normalizador/migração técnica'},
];
function filesIn(p){const abs=path.join(root,p);if(!fs.existsSync(abs))return [];const st=fs.statSync(abs);if(st.isFile())return [abs];let out=[];for(const ent of fs.readdirSync(abs,{withFileTypes:true})){if(ignoreDirs.has(ent.name))continue;const fp=path.join(abs,ent.name);if(ent.isDirectory())out=out.concat(filesIn(path.relative(root,fp)));else out.push(fp);}return out;}
function allowed(file,line){const rel=file.replace(root+path.sep,'').replace(/\\/g,'/');return allow.some(a=>(rel===a.file||rel.startsWith(a.file.replace(/\/$/,'')+'/')||a.file==='tests'&&rel.startsWith('tests/'))&&a.re.test(line));}
const failures=[];for(const f of [...new Set(scanRoots.flatMap(filesIn))]){let text;try{text=fs.readFileSync(f,'utf8');}catch(_){continue;} if(f.includes(`${path.sep}node_modules${path.sep}`))continue; text.split(/\r?\n/).forEach((line,i)=>{if(forbidden.test(line)&&!allowed(f,line))failures.push(`${path.relative(root,f)}:${i+1}: ${line.trim().slice(0,220)}`);});}
if(failures.length){console.error('Marca legada visível encontrada:\n'+failures.join('\n'));process.exit(1);}console.log('validate-legacy-identity-contract: PASS');

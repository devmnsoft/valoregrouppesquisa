#!/usr/bin/env node
'use strict';
const fs=require('fs');
const path=require('path');
const root=process.cwd();
const required=['config.js','config/config.production.js'];
const failures=[];
for(const file of required){const text=fs.readFileSync(path.join(root,file),'utf8');
  if(!/productName:\s*'Valora Insight™'/.test(text))failures.push(`${file}: productName canônico ausente`);
  if(!/whatsappDigits:\s*'5591992545353'/.test(text))failures.push(`${file}: whatsappDigits inválido`);
  if(!/whatsappDisplay:\s*'\+55 91 99254-5353'/.test(text))failures.push(`${file}: whatsappDisplay inválido`);
  if(!/whatsappUrl:\s*'https:\/\/wa\.me\/5591992545353'/.test(text))failures.push(`${file}: whatsappUrl inválida`);
}
const active=['app.js','firebase-repository.js','firebase-init.js','repository.js','local-repository.js','api-repository.js','gateway-client.js','notification-service.js','report-service.js','chatbot-service.js','chatbot-knowledge-base.js','pdf.js','data-normalization.js','role-definitions.js','functions/index.js'];
for(const file of active.filter(f=>fs.existsSync(f))){const text=fs.readFileSync(file,'utf8');const visible=text.split('\n').filter(line=>!(/LEGACY_STORE_KEYS|valoraPulseFinal800|startsWith\('ValoraPulse'\)|startsWith\('valoraPulse'\)|user-agent.*ValoraPulse/.test(line))).join('\n');if(/Valora\s*Pulse/i.test(visible))failures.push(`${file}: marca legada em código ativo`);for(const match of text.matchAll(/https:\/\/wa\.me\/([^?'"`\s$]+)/g)){if(match[1]&&!/^\d+$/.test(match[1]))failures.push(`${file}: wa.me literal não numérico`);}}
if(failures.length){console.error(failures.map(x=>`FAIL: ${x}`).join('\n'));process.exit(1)}
console.log('legacy identity contract: PASS');

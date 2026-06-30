#!/usr/bin/env node
const fs=require('fs');
const files=['app.js','firebase-repository.js','repository.js','api-repository.js'];
const all=files.map(f=>fs.existsSync(f)?fs.readFileSync(f,'utf8'):'').join('\n');
for(const t of ['organization','company','settings','publicName','legalName','slug','planId','status']){if(!all.includes(t)){console.error('Contrato de cliente sem '+t);process.exit(1);}}
console.log('Contrato runtime de cliente validado.');

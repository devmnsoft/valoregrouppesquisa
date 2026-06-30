#!/usr/bin/env node
const fs=require('fs');
const source=fs.readFileSync('firebase-repository.js','utf8')+'\n'+fs.readFileSync('functions/index.js','utf8');
const checks=['createUserProfile','createCompanyUser','admin.auth().createUser',"users').doc(authUser.uid",'setCustomUserClaims','generatePasswordResetLink'];
for(const t of checks){if(!source.includes(t)){console.error('Contrato de usuário falhou: '+t);process.exit(1);}}
console.log('Contrato runtime de usuário validado.');

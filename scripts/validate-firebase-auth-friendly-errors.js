#!/usr/bin/env node
const fs=require('fs');
const s=fs.readFileSync('firebase-repository.js','utf8')+'\n'+fs.readFileSync('app.js','utf8');
for(const t of ['auth/invalid-login-credentials','auth/user-not-found','auth/wrong-password','auth/invalid-email','auth/user-disabled','auth/too-many-requests','auth/network-request-failed','auth/api-key-not-valid','auth/operation-not-allowed','profile-missing','inactive-user','E-mail ou senha inválidos.','Serviço de autenticação indisponível.']){if(!s.includes(t)){console.error('Erro amigável ausente: '+t);process.exit(1);}}
console.log('Erros amigáveis Firebase Auth validados.');

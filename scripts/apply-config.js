#!/usr/bin/env node
const fs=require('fs');const path=require('path');
const env=(process.argv[process.argv.indexOf('--env')+1]||'').trim();
const map={production:'config.production.js',local:'config.local.js','local-firebase':'config.local-firebase.js'};
if(!map[env]){console.error('Uso: node scripts/apply-config.js --env production|local|local-firebase');process.exit(1)}
const root=path.resolve(__dirname,'..');fs.copyFileSync(path.join(root,'config',map[env]),path.join(root,'config.js'));console.log(`Configuração aplicada: ${env} -> config.js`);

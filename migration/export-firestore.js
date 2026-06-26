#!/usr/bin/env node
const fs=require('fs');const path=require('path');
const collections=['settings','plans','modules','companies','organizations','users','forms','surveys','responses','invitations','communications','actionPlans','certificates'];
const outDir=path.join(__dirname,'export');fs.mkdirSync(outDir,{recursive:true});
const dryRun=process.argv.includes('--dry-run')||!process.env.GOOGLE_APPLICATION_CREDENTIALS;
(async()=>{const result={ok:true,mode:dryRun?'dry-run':'export',collections:{},createdAt:new Date().toISOString()};
if(dryRun){for(const c of collections){result.collections[c]={count:0,file:`migration/export/${c}.json`,dryRun:true};fs.writeFileSync(path.join(outDir,`${c}.json`),'[]\n');}fs.writeFileSync(path.join(outDir,'manifest.json'),JSON.stringify(result,null,2));console.log('export-firestore: dry-run seguro concluído.');return;}
const admin=require('firebase-admin');admin.initializeApp({credential:admin.credential.applicationDefault()});const db=admin.firestore();
for(const c of collections){const snap=await db.collection(c).get();const rows=snap.docs.map(d=>({id:d.id,...d.data()}));fs.writeFileSync(path.join(outDir,`${c}.json`),JSON.stringify(rows,null,2));result.collections[c]={count:rows.length,file:`migration/export/${c}.json`};}
fs.writeFileSync(path.join(outDir,'manifest.json'),JSON.stringify(result,null,2));console.log('export-firestore: exportação concluída.');})().catch(e=>{console.error(e.message);process.exit(1);});

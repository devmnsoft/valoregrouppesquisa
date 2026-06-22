#!/usr/bin/env node
'use strict';
const admin=require('firebase-admin');
const crypto=require('crypto');
const {parseArgs,readJson,writeJson,stamp,normalizeExport,collectionEntries,idOf}=require('./firebase-seed-utils');
const args=parseArgs();
if(!args.file||!args.project){console.error('Uso: node scripts/import-firestore-seed.js --file export.json --project PROJECT --dry-run|--apply [--merge] [--overwrite --confirm-overwrite IMPORTAR] [--include-responses] [--create-auth-users] [--send-password-reset] [--backup]');process.exit(1);}
const apply=!!args.apply; if(!apply&&!args['dry-run']) args['dry-run']=true;
if(args.overwrite&&args['confirm-overwrite']!=='IMPORTAR'){console.error('--overwrite exige --confirm-overwrite IMPORTAR');process.exit(2);}
admin.initializeApp({projectId:args.project});
const db=admin.firestore();
const auth=admin.auth();
const exported=normalizeExport(readJson(args.file),{includeResponses:!!args['include-responses']&&!args['skip-responses']});
const entries=collectionEntries(exported,{includeResponses:!!args['include-responses']&&!args['skip-responses'],legacyCompanies:true,markResponsesDemo:true});
async function backup(){const cols=['settings','modules','plans','organizations','companies','users','forms','surveys','responses','invitations','invoices','actionPlans','notifications','knowledgeBase','supportCategories','supportSlaPolicies','supportTickets','supportMessages','integrations','webhooks','apiKeys','logs'];const out={version:'firestore-backup-v1',project:args.project,exportedAt:new Date().toISOString(),data:{}};for(const c of cols){const snap=await db.collection(c).get();out.data[c]=snap.docs.map(d=>({id:d.id,...d.data()}));}const file=`backups/firestore-backup-${stamp()}.json`;writeJson(file,out);console.log(`Backup criado: ${file}`);}
async function upsertAuthUser(u){if(!u.email)return null;let rec;try{rec=await auth.getUserByEmail(u.email);}catch(e){if(e.code!=='auth/user-not-found')throw e;}
 const create=!rec; if(create){rec=await auth.createUser({email:u.email,emailVerified:false,displayName:u.name||u.email,disabled:u.status==='inactive',password:`Valora@2026-${crypto.randomBytes(12).toString('base64url')}`});} else {await auth.updateUser(rec.uid,{displayName:u.name||rec.displayName,disabled:u.status==='inactive'});} await auth.setCustomUserClaims(rec.uid,{role:u.role||'participante',companyId:u.companyId||''});
 const doc={uid:rec.uid,legacyId:u.id||u.uid||'',name:u.name||'',email:u.email,role:u.role||'participante',companyId:u.companyId||'',phone:u.phone||'',department:u.department||'',position:u.position||'',status:u.status||'active',receivesEmail:u.receivesEmail!==false,portalAccess:u.portalAccess!==false,createdAt:u.createdAt||new Date().toISOString(),updatedAt:new Date().toISOString(),migratedAt:new Date().toISOString()};
 await db.collection('users').doc(rec.uid).set(doc,{merge:!args.overwrite});
 if(args['send-password-reset']){const link=await auth.generatePasswordResetLink(u.email);console.log(`Reset gerado para ${u.email}: ${link}`);} return {uid:rec.uid,created:create};}
(async()=>{const counts={};entries.forEach(e=>counts[e.collection]=(counts[e.collection]||0)+1);console.log(args.apply?'APPLY':'DRY-RUN',counts); if(!apply)return;
 if(args.backup)await backup();
 if(args['create-auth-users']&&!args['skip-users']){let created=0,updated=0;for(const u of exported.data.users||[]){const r=await upsertAuthUser(u);if(r?.created)created++;else if(r)updated++;}console.log(`Auth/users: ${created} criados, ${updated} atualizados/localizados.`);} 
 const batchSize=400; for(let i=0;i<entries.length;i+=batchSize){const batch=db.batch();for(const e of entries.slice(i,i+batchSize)){batch.set(db.collection(e.collection).doc(e.id),{...e.data,migratedAt:new Date().toISOString()},{merge:!args.overwrite});}await batch.commit();}
 await db.collection('settings').doc('seed').set({lastMigrationAt:new Date().toISOString(),sourceFile:args.file,counts},{merge:true}); console.log('Importação concluída.');
})().catch(e=>{console.error(e);process.exit(1);});

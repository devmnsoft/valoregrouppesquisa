#!/usr/bin/env node
const fs=require('fs');const path=require('path');const admin=require('firebase-admin');
const projectId=process.env.FIREBASE_PROJECT_ID||process.env.GCLOUD_PROJECT||'gestordepesquisa';
const out=process.argv[2]||path.join('migration','firestore-export.json');
const collections=['plans','organizations','companies','users','forms','surveys','responses','certificates','communications','actionPlans','notifications','knowledgeBase','supportTickets'];
(async()=>{admin.initializeApp({projectId});const db=admin.firestore();const data={projectId,exportedAt:new Date().toISOString(),collections:{}};for(const c of collections){const snap=await db.collection(c).get();data.collections[c]=snap.docs.map(d=>({id:d.id,...d.data()}));console.log(`${c}: ${snap.size}`)}fs.writeFileSync(out,JSON.stringify(data,null,2));console.log(`exported ${out}`);})().catch(e=>{console.error(e);process.exit(1)});

#!/usr/bin/env node
'use strict';
const admin=require('firebase-admin');
const {parseArgs,writeJson,stamp,COLLECTIONS}=require('./firebase-seed-utils');
const args=parseArgs(); if(!args.project){console.error('Uso: node scripts/backup-firestore.js --project PROJECT');process.exit(1);}admin.initializeApp({projectId:args.project});
(async()=>{const db=admin.firestore();const out={version:'firestore-backup-v1',project:args.project,exportedAt:new Date().toISOString(),data:{}};for(const c of COLLECTIONS){const snap=await db.collection(c).get();out.data[c]=snap.docs.map(d=>({id:d.id,...d.data()}));}const file=`backups/firestore-backup-${stamp()}.json`;writeJson(file,out);console.log(`Backup criado: ${file}`);})().catch(e=>{console.error(e);process.exit(1);});

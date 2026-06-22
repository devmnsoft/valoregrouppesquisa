#!/usr/bin/env node
'use strict';
const admin = require('firebase-admin');
const projectArgIndex = process.argv.indexOf('--project');
const projectId = projectArgIndex >= 0 && process.argv[projectArgIndex + 1] ? process.argv[projectArgIndex + 1] : 'gestordepesquisa';
async function count(db, col){ const s = await db.collection(col).count().get(); return s.data().count; }
async function assert(name, fn){ try { const detail = await fn(); console.log(`OK ${name}${detail?`: ${detail}`:''}`); return true; } catch(e) { console.error(`FAIL ${name}: ${e.message}`); return false; } }
async function main(){ admin.initializeApp({projectId}); const db=admin.firestore(); let adminUid=''; const checks=[];
checks.push(await assert('admin_valora em Firestore e Auth', async()=>{ const qs=await db.collection('users').where('role','==','admin_valora').limit(1).get(); if(qs.empty) throw new Error('users com role admin_valora não encontrado'); adminUid=qs.docs[0].id; await admin.auth().getUser(adminUid); return adminUid; }));
checks.push(await assert('custom claims do admin', async()=>{ const u=await admin.auth().getUser(adminUid); if(u.customClaims?.role!=='admin_valora' || u.customClaims?.companyId!=='') throw new Error('claims inválidas'); return JSON.stringify(u.customClaims); }));
for (const [label,col,min] of [['planos','plans',4],['módulos','modules',15],['settings/global','settings',1],['organização','organizations',1],['formulário','forms',1],['pesquisa ativa','surveys',1]]) checks.push(await assert(label, async()=>{ const c= col==='settings' ? (await db.collection('settings').doc('global').get()).exists : await count(db,col) >= min; if(!c) throw new Error(`mínimo não encontrado em ${col}`); return col==='settings'?'settings/global':String(await count(db,col)); }));
checks.push(await assert('survey aponta para form existente', async()=>{ const qs=await db.collection('surveys').where('status','==','active').limit(10).get(); for(const d of qs.docs){ const s=d.data(); if(s.formId && (await db.collection('forms').doc(s.formId).get()).exists) return `${d.id} -> ${s.formId}`; } throw new Error('nenhuma pesquisa ativa com form existente'); }));
checks.push(await assert('planos apontam para módulos existentes', async()=>{ const mods=new Set((await db.collection('modules').get()).docs.map(d=>d.id)); const plans=(await db.collection('plans').get()).docs; for(const p of plans){ for(const m of (p.data().enabledModules||[])){ if(!mods.has(m)) throw new Error(`${p.id} referencia módulo inexistente ${m}`); } } return `${plans.length} planos válidos`; }));
console.log(checks.every(Boolean) ? 'VALIDAÇÃO PRD: OK - app não deve abrir vazio.' : 'VALIDAÇÃO PRD: FALHOU'); process.exit(checks.every(Boolean)?0:1); }
main().catch(e=>{console.error(e);process.exit(1);});

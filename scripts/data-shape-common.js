'use strict';
const fs=require('fs');const path=require('path');const admin=require('firebase-admin');const norm=require('../data-normalization');
const root=path.resolve(__dirname,'..');
const arrayFields=['plans','modules','companies','organizations','users','forms','surveys','responses','knowledgeBase','supportCategories','supportSlaPolicies'];
function parseArgs(argv=process.argv.slice(2)){const out={};for(let i=0;i<argv.length;i++){const a=argv[i];if(!a.startsWith('--'))continue;const k=a.slice(2);if(['dry-run','apply','backup','json'].includes(k))out[k]=true;else out[k]=argv[++i];}return out;}
function shape(v){if(Array.isArray(v))return 'array';if(v===null)return 'null';return typeof v;}
function reportForData(data){const state=norm.normalizeAppState(data||{});const checks={};checks['settings.faq']={expected:'array',actual:shape(data?.settings?.faq),ok:Array.isArray(state.settings.faq),items:state.settings.faq.length};for(const f of arrayFields)checks[f]={expected:'array',actual:shape(data?.[f]),ok:Array.isArray(state[f]),items:state[f].length};return {createdAt:new Date().toISOString(),checks,normalized:state,errors:Object.entries(checks).filter(([,v])=>!v.ok).map(([k,v])=>`${k} esperado array, recebido ${v.actual}`)};}
async function loadFirestore(project){if(!admin.apps.length)admin.initializeApp({projectId:project});const db=admin.firestore();const data={};for(const c of arrayFields){const s=await db.collection(c).get();data[c]=s.docs.map(d=>({id:d.id,...d.data()}));}
const settings=await db.collection('settings').get();data.settings=settings.empty?{}:settings.docs.reduce((acc,d)=>d.id==='public'?{...acc,...d.data()}:{...acc,[d.id]:d.data()},{});return {db,data};}
function writeAudit(report){fs.mkdirSync(path.join(root,'reports'),{recursive:true});fs.writeFileSync(path.join(root,'reports','data-shape-valorapesquisa.json'),JSON.stringify(report,null,2));}
module.exports={root,parseArgs,reportForData,loadFirestore,writeAudit,norm,arrayFields};
